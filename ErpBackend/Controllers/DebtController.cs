using ErpBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebtController : ControllerBase
{
    private readonly AppDbContext _context;

    public DebtController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetDebtOverview()
    {
        // Nhóm các hóa đơn chưa thanh toán hết theo Khách hàng
        var overview = await _context.Invoices
            .Where(i => i.Status != "Paid" && i.TotalAmount > i.PaidAmount)
            .GroupBy(i => i.CustomerName)
            .Select(g => new
            {
                CustomerName = g.Key,
                TotalDebt = g.Sum(i => i.TotalAmount - i.PaidAmount),
                InvoiceCount = g.Count()
            })
            .OrderByDescending(x => x.TotalDebt)
            .ToListAsync();

        return Ok(overview);
    }

    [HttpGet("aging")]
    public async Task<IActionResult> GetDebtAging()
    {
        var now = DateTime.UtcNow;

        var unpaidInvoices = await _context.Invoices
            .Where(i => i.Status != "Paid" && i.TotalAmount > i.PaidAmount)
            .ToListAsync();

        // Kể chuyện: Phân tích nợ quá hạn
        var aging = new
        {
            Under30Days = unpaidInvoices
                .Where(i => (now - i.InvoiceDate).TotalDays <= 30)
                .Sum(i => i.TotalAmount - i.PaidAmount),
            Between30And60Days = unpaidInvoices
                .Where(i => (now - i.InvoiceDate).TotalDays > 30 && (now - i.InvoiceDate).TotalDays <= 60)
                .Sum(i => i.TotalAmount - i.PaidAmount),
            Over60Days = unpaidInvoices
                .Where(i => (now - i.InvoiceDate).TotalDays > 60)
                .Sum(i => i.TotalAmount - i.PaidAmount)
        };

        return Ok(aging);
    }
}
