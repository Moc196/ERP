using ErpBackend.Data;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AlertCheckerService _checker;

    public AlertsController(AppDbContext context, AlertCheckerService checker)
    {
        _context = context;
        _checker = checker;
    }

    // Lấy danh sách alerts (chưa đọc để hiện badge, có thể lọc)
    [HttpGet]
    public async Task<IActionResult> GetAlerts([FromQuery] bool unreadOnly = false)
    {
        var query = _context.AlertNotifications.AsQueryable();
        if (unreadOnly) query = query.Where(a => !a.IsRead);

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new {
                a.Id, a.Type, a.Severity, a.Title, a.Message, a.IsRead, a.CreatedAt
            })
            .ToListAsync();

        var unreadCount = await _context.AlertNotifications.CountAsync(a => !a.IsRead);
        return Ok(new { unreadCount, alerts });
    }

    // Đánh dấu 1 alert đã đọc
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var alert = await _context.AlertNotifications.FindAsync(id);
        if (alert == null) return NotFound();
        alert.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok();
    }

    // Đánh dấu tất cả đã đọc
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _context.AlertNotifications
            .Where(a => !a.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsRead, true));
        return Ok(new { message = "Đã đánh dấu tất cả là đã đọc" });
    }

    // Admin: trigger kiểm tra thủ công ngay lập tức
    [Authorize(Roles = "Admin")]
    [HttpPost("check-now")]
    public async Task<IActionResult> CheckNow()
    {
        await _checker.RunAllChecksAsync();
        return Ok(new { message = "Đã kiểm tra xong. Xem danh sách alerts để biết kết quả." });
    }

    // Admin: test gửi email thủ công
    [Authorize(Roles = "Admin")]
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromServices] EmailNotifier email)
    {
        await email.SendAsync(
            "Test kết nối email",
            "ERP.Vibe Alert System đang hoạt động!\n\nNếu bạn nhận được email này, hệ thống gửi thông báo email đã được cấu hình đúng."
        );
        return Ok(new { message = "Đã gửi test email. Kiểm tra hộp thư của bạn (có thể vào Spam)." });
    }

    // Admin: test gửi telegram thủ công
    [Authorize(Roles = "Admin")]
    [HttpPost("test-telegram")]
    public async Task<IActionResult> TestTelegram([FromServices] TelegramNotifier telegram)
    {
        await telegram.SendAsync(
            "🚀 ERP.Vibe: Đây là tin nhắn kiểm tra kết nối Telegram!\nNếu bạn thấy tin nhắn này, hệ thống thông báo đã được cấu hình chính xác."
        );
        return Ok(new { message = "Đã gửi test telegram. Hãy kiểm tra ứng dụng Telegram của bạn." });
    }
}
