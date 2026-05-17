using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;
using ErpBackend.Dtos;
using ErpBackend.Repositories;
using Microsoft.EntityFrameworkCore;

using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly AppDbContext _context;
    private readonly TelegramNotifier _telegram;
    private readonly EmailNotifier _email;

    public InvoicesController(
        IInvoiceRepository invoiceRepository, 
        AppDbContext context,
        TelegramNotifier telegram,
        EmailNotifier email)
    {
        _invoiceRepository = invoiceRepository;
        _context = context;
        _telegram = telegram;
        _email = email;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices()
    {
        var invoices = await _invoiceRepository.GetAllAsync();
        return Ok(invoices);
    }

    [Authorize(Policy = "invoice.create")]
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var invoice = await _invoiceRepository.CreateInvoiceAsync(dto, username);

            // Kiểm tra số lượng lớn (> 100) để cảnh báo tức thì - Chỉ báo khi chưa thanh toán xong
            if (dto.Items.Any(i => i.Quantity > 100) && invoice.Status != "Paid")
            {
                var msg = $"🚨 CẢNH BÁO: ĐƠN HÀNG LỚN\n" +
                          $"Hóa đơn: {invoice.InvoiceNumber}\n" +
                          $"Khách hàng: {invoice.CustomerName}\n" +
                          $"Số tiền: {invoice.TotalAmount:N0}₫\n" +
                          $"Phát hiện sản phẩm mua số lượng lớn (> 100)!";

                // Chạy ngầm để không block UI chốt đơn
                _ = Task.Run(() => _telegram.SendAsync(msg));
                _ = Task.Run(() => _email.SendAsync("Cảnh báo đơn hàng lớn", msg));
            }

            return CreatedAtAction(nameof(GetInvoices), new { id = invoice.Id }, invoice);
        }
        catch (Exception ex)
        {
            // Trả về lỗi 400 Bad Request kèm thông báo chi tiết (VD: thiếu tồn kho)
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Policy = "invoice.payment")]
    [HttpPost("{invoiceId}/payments")]
    public async Task<IActionResult> CreatePayment(int invoiceId, [FromBody] PaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var payment = await _invoiceRepository.AddPaymentAsync(invoiceId, dto, username);
            return Ok(new { message = "Thanh toán thành công!", payment });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{invoiceId}/payments")]
    public async Task<IActionResult> GetPayments(int invoiceId)
    {
        var payments = await _context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new {
                p.Id, p.Amount, p.PaymentMethod, p.ProcessedBy, p.PaymentDate
            })
            .ToListAsync();
        return Ok(payments);
    }

    [Authorize]
    [HttpGet("{invoiceId}/pdf")]
    public async Task<IActionResult> ExportInvoicePdf(int invoiceId, [FromServices] PdfGeneratorService pdfService)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return NotFound();

        var html = $@"
        <html>
        <head>
            <style>
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; }}
                .invoice-box {{ max-width: 800px; margin: auto; padding: 30px; border: 1px solid #eee; box-shadow: 0 0 10px rgba(0, 0, 0, .15); font-size: 16px; line-height: 24px; }}
                .header {{ display: flex; justify-content: space-between; border-bottom: 2px solid #6366f1; padding-bottom: 20px; margin-bottom: 20px; }}
                .title {{ color: #6366f1; font-size: 32px; font-weight: bold; }}
                table {{ width: 100%; text-align: left; border-collapse: collapse; }}
                th {{ background: #f8fafc; padding: 12px; border-bottom: 1px solid #e2e8f0; }}
                td {{ padding: 12px; border-bottom: 1px solid #f1f5f9; }}
                .total {{ text-align: right; margin-top: 30px; font-size: 20px; font-weight: bold; color: #6366f1; }}
                .footer {{ margin-top: 50px; text-align: center; font-size: 12px; color: #94a3b8; }}
            </style>
        </head>
        <body>
            <div class='invoice-box'>
                <div class='header'>
                    <div>
                        <div class='title'>HÓA ĐƠN BÁN HÀNG</div>
                        <div>Số HĐ: {invoice.InvoiceNumber}</div>
                        <div>Ngày: {invoice.InvoiceDate:dd/MM/yyyy HH:mm}</div>
                        <div>Tiền tệ: {invoice.CurrencyCode}</div>
                    </div>
                    <div style='text-align: right;'>
                        <div style='font-weight: bold;'>Cửa Hàng ERP</div>
                        <div>Địa chỉ: 123 Đường ABC, Hà Nội</div>
                        <div>SĐT: 0123 456 789</div>
                    </div>
                </div>

                <div style='margin-bottom: 30px;'>
                    <strong>Khách hàng:</strong> {invoice.CustomerName}<br/>
                    <strong>Người bán:</strong> {invoice.CreatedBy}
                </div>

                <table>
                    <thead>
                        <tr>
                            <th>Sản phẩm</th>
                            <th style='text-align: center;'>SL</th>
                            <th style='text-align: right;'>Đơn giá</th>
                            <th style='text-align: right;'>Thành tiền</th>
                        </tr>
                    </thead>
                    <tbody>
                        {string.Join("", invoice.Items.Select(item => $@"
                         <tr>
                            <td>{item.Product?.Name ?? "Sản phẩm không tên"}</td>
                            <td style='text-align: center;'>{item.Quantity}</td>
                            <td style='text-align: right;'>{(invoice.CurrencyCode == "VND" ? item.UnitPrice.ToString("N0") + "₫" : item.UnitPrice.ToString("N2") + " " + invoice.CurrencyCode)}</td>
                            <td style='text-align: right;'>{(invoice.CurrencyCode == "VND" ? (item.UnitPrice * item.Quantity).ToString("N0") + "₫" : (item.UnitPrice * item.Quantity).ToString("N2") + " " + invoice.CurrencyCode)}</td>
                        </tr>"))}
                    </tbody>
                </table>

                <div class='total'>
                    Tổng tiền: {(invoice.CurrencyCode == "VND" ? invoice.TotalAmount.ToString("N0") + "₫" : invoice.TotalAmount.ToString("N2") + " " + invoice.CurrencyCode)}
                </div>

                <div class='footer'>
                    Cảm ơn quý khách đã tin tưởng và sử dụng dịch vụ của chúng tôi!<br/>
                    Hóa đơn này được tạo tự động bởi hệ thống ERP
                </div>
            </div>
        </body>
        </html>";

        var pdfBytes = await pdfService.GeneratePdfFromHtmlAsync(html);
        return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceNumber}.pdf");
    }
}
