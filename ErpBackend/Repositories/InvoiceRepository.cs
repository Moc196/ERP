using ErpBackend.Data;
using ErpBackend.Dtos;
using ErpBackend.Entities;
using Microsoft.EntityFrameworkCore;

using ErpBackend.Services;

namespace ErpBackend.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ExchangeRateService _exchangeRateService;
    private readonly AccountingService _accountingService;

    public InvoiceRepository(AppDbContext context, ICurrentUserService currentUserService, ExchangeRateService exchangeRateService, AccountingService accountingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _exchangeRateService = exchangeRateService;
        _accountingService = accountingService;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        // Nhờ Global Filter trong AppDbContext, kết quả sẽ tự động lọc theo chi nhánh nếu không phải Admin
        return await _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.Product)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<Invoice> CreateInvoiceAsync(CreateInvoiceDto dto, string createdBy = "system")
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var invoice = new Invoice
            {
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                InvoiceDate = DateTime.UtcNow,
                Status = "Unpaid",
                CreatedBy = createdBy,
                BranchId = _currentUserService.BranchId // Gán chi nhánh tự động từ user hiện tại
            };

            // Nếu có ID khách hàng, tự động lấy tên chuẩn xác nhất từ DB
            if (dto.CustomerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(dto.CustomerId.Value);
                if (customer != null)
                {
                    invoice.CustomerName = customer.Name;
                }
            }

            // Sinh mã hóa đơn tự động (VD: HD001)
            var count = await _context.Invoices.CountAsync();
            invoice.InvoiceNumber = $"HD{(count + 1):D3}";

            decimal totalAmount = 0;

            // Bước 2: Duyệt qua từng món hàng khách mua
            foreach (var itemDto in dto.Items)
            {
                // Tìm trong kho xem có sản phẩm này không
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    throw new Exception($"Không tìm thấy sản phẩm có ID: {itemDto.ProductId}");

                // Lấy tồn kho tại chi nhánh hiện tại
                var branchId = _currentUserService.BranchId ?? 0;
                var branchStock = await _context.BranchStocks
                    .FirstOrDefaultAsync(bs => bs.ProductId == itemDto.ProductId && bs.BranchId == branchId);
                
                if (branchStock == null)
                    throw new Exception($"Sản phẩm '{product.Name}' không có sẵn tại chi nhánh này!");

                // Kiểm tra xem số lượng tồn kho có đủ không
                if (branchStock.Quantity < itemDto.Quantity)
                    throw new Exception($"Sản phẩm '{product.Name}' không đủ tồn kho tại chi nhánh này! (Còn: {branchStock.Quantity}, Yêu cầu: {itemDto.Quantity})");

                // Trừ tồn kho tại chi nhánh
                branchStock.Quantity -= itemDto.Quantity;

                // Ghi lại câu chuyện xuất kho
                var stockTx = new StockTransaction
                {
                    ProductId = product.Id,
                    BranchId = branchId,
                    Quantity = itemDto.Quantity,
                    Type = "Export",
                    ReferenceId = invoice.InvoiceNumber,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };
                _context.StockTransactions.Add(stockTx);

                // Thêm vào chi tiết hóa đơn
                var invoiceItem = new InvoiceItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price
                };

                invoice.Items.Add(invoiceItem);

                // Cộng dồn tính tổng tiền
                totalAmount += itemDto.Quantity * product.Price;
            }

            // Bước 3: Cập nhật tổng tiền và lưu vào Database
            invoice.CurrencyCode = dto.CurrencyCode;
            invoice.ExchangeRate = await _exchangeRateService.GetRateAsync(dto.CurrencyCode);
            invoice.TotalAmount = totalAmount;
            invoice.TotalAmountVND = totalAmount * invoice.ExchangeRate;
            
            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            // Tự động hạch toán kế toán (Nợ 131 / Có 511)
            await _accountingService.AutoPostInvoiceAsync(invoice);

            // Tự động hạch toán Giá vốn (Nợ 632 / Có 156)
            await _accountingService.AutoPostCOGSAsync(invoice);

            await _context.SaveChangesAsync();

            // Kể chuyện: Giao dịch thành công, chốt (Commit)!
            await transaction.CommitAsync();

            return invoice;
        }
        catch (Exception)
        {
            // Giao dịch thất bại, hoàn tác mọi thay đổi (Rollback)!
            await transaction.RollbackAsync();
            throw; // Ném lỗi ra ngoài cho Controller xử lý
        }
    }

    public async Task<Payment> AddPaymentAsync(int invoiceId, PaymentDto dto, string processedBy = "system")
    {
        // Kể chuyện: Khách mang tiền đến trả nợ
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                throw new Exception($"Không tìm thấy hóa đơn ID: {invoiceId}");

            if (invoice.Status == "Paid")
                throw new Exception("Hóa đơn này đã được thanh toán đầy đủ!");

            var remaining = invoice.TotalAmount - invoice.PaidAmount;
            if (dto.Amount <= 0)
                throw new Exception("Số tiền thanh toán phải lớn hơn 0!");
            if (dto.Amount > remaining)
                throw new Exception($"Số tiền nhập ({dto.Amount:N0}₫) vượt quá số còn nợ ({remaining:N0}₫)!");

            // Cập nhật số tiền đã trả
            invoice.PaidAmount += dto.Amount;

            // Kiểm tra xem đã trả đủ chưa
            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = "Paid";
                // Nếu khách trả dư, có thể cấn trừ hoặc ghi nhận dư (ở đây đơn giản là gán lại Status)
            }

            // Ghi biên lai thu tiền
            var payment = new Payment
            {
                InvoiceId = invoiceId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ProcessedBy = processedBy,
                PaymentDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Tự động hạch toán kế toán (Nợ 111 / Có 131)
            await _accountingService.AutoPostPaymentAsync(payment, invoice.CustomerName, invoice.BranchId);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return payment;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
