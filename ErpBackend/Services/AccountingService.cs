using ErpBackend.Data;
using ErpBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public class AccountingService
{
    private readonly AppDbContext _context;

    public AccountingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tự động hạch toán khi phát sinh Hóa đơn bán hàng
    /// </summary>
    public async Task AutoPostInvoiceAsync(Invoice invoice)
    {
        // 1. Tìm tài khoản Phải thu khách hàng (Mặc định là 131)
        var receivableAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Code == "131");

        if (receivableAccount == null)
            throw new Exception("Không tìm thấy tài khoản Phải thu khách hàng (131) trong hệ thống!");

        // 2. Tạo Phiếu kế toán (Journal Entry)
        var entry = new JournalEntry
        {
            BranchId = invoice.BranchId ?? 1,
            EntryDate = invoice.InvoiceDate,
            Reference = invoice.InvoiceNumber,
            Description = $"Hạch toán doanh thu hóa đơn {invoice.InvoiceNumber} - Khách hàng: {invoice.CustomerName}"
        };

        // 3. Dòng Nợ (Debit): Phải thu khách hàng
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = receivableAccount.Id,
            Debit = invoice.TotalAmount,
            Credit = 0
        });

        // 4. Các dòng Có (Credit): Doanh thu bán hàng
        // Duyệt qua từng sản phẩm trong hóa đơn để lấy tài khoản doanh thu tương ứng
        foreach (var item in invoice.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            
            // Lấy tài khoản doanh thu từ sản phẩm, nếu không có thì lấy mặc định 511
            int incomeAccountId;
            if (product?.IncomeAccountId != null)
            {
                incomeAccountId = product.IncomeAccountId.Value;
            }
            else
            {
                var defaultRevenueAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "511");
                if (defaultRevenueAcc == null) throw new Exception("Không tìm thấy tài khoản Doanh thu (511)!");
                incomeAccountId = defaultRevenueAcc.Id;
            }

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = incomeAccountId,
                Debit = 0,
                Credit = item.Quantity * item.UnitPrice
            });
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Tự động hạch toán khi có Thanh toán
    /// </summary>
    public async Task AutoPostPaymentAsync(Payment payment, string customerName, int? branchId)
    {
        // Nợ 111 (Tiền mặt) / Có 131 (Phải thu)
        var cashAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "111");
        var receivableAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "131");

        if (cashAccount == null || receivableAccount == null)
            throw new Exception("Thiếu tài khoản 111 hoặc 131 để hạch toán thanh toán!");

        var entry = new JournalEntry
        {
            BranchId = branchId ?? 1,
            EntryDate = payment.PaymentDate,
            Reference = $"PAY-{payment.Id}",
            Description = $"Thu tiền thanh toán khách hàng: {customerName}"
        };

        entry.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = payment.Amount, Credit = 0 });
        entry.Lines.Add(new JournalEntryLine { AccountId = receivableAccount.Id, Debit = 0, Credit = payment.Amount });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Tự động hạch toán khi Nhập kho (Nợ 156 / Có 331)
    /// </summary>
    public async Task AutoPostStockImportAsync(Product product, int quantity, int? branchId, decimal purchasePrice)
    {
        var inventoryAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "156");
        var payableAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "331");

        if (inventoryAcc == null || payableAcc == null)
            throw new Exception("Thiếu tài khoản 156 hoặc 331 để hạch toán nhập kho!");

        decimal importValue = purchasePrice * quantity;

        var entry = new JournalEntry
        {
            BranchId = branchId ?? 1,
            EntryDate = DateTime.UtcNow,
            Reference = "IMPORT",
            Description = $"Nhập kho sản phẩm: {product.Name} (SL: {quantity})"
        };

        entry.Lines.Add(new JournalEntryLine { AccountId = inventoryAcc.Id, Debit = importValue, Credit = 0 });
        entry.Lines.Add(new JournalEntryLine { AccountId = payableAcc.Id, Debit = 0, Credit = importValue });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Tự động hạch toán Giá vốn hàng bán (Nợ 632 / Có 156)
    /// </summary>
    public async Task AutoPostCOGSAsync(Invoice invoice)
    {
        var cogsAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "632");
        var inventoryAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "156");

        if (cogsAcc == null || inventoryAcc == null)
            throw new Exception("Thiếu tài khoản 632 hoặc 156 để hạch toán giá vốn!");

        var entry = new JournalEntry
        {
            BranchId = invoice.BranchId ?? 1,
            EntryDate = invoice.InvoiceDate,
            Reference = invoice.InvoiceNumber,
            Description = $"Hạch toán giá vốn hóa đơn {invoice.InvoiceNumber}"
        };

        decimal totalCogs = 0;
        foreach (var item in invoice.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            // Sử dụng giá vốn bình quân gia quyền đã lưu trong Product
            decimal itemCogs = item.Quantity * (product?.CostPrice ?? 0);
            totalCogs += itemCogs;
        }

        entry.Lines.Add(new JournalEntryLine { AccountId = cogsAcc.Id, Debit = totalCogs, Credit = 0 });
        entry.Lines.Add(new JournalEntryLine { AccountId = inventoryAcc.Id, Debit = 0, Credit = totalCogs });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }
}
