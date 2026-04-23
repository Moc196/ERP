using ErpBackend.Data;
using ErpBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Services;

public class AlertCheckerService
{
    private readonly AppDbContext _context;
    private readonly TelegramNotifier _telegram;
    private readonly EmailNotifier _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertCheckerService> _logger;

    public AlertCheckerService(
        AppDbContext context,
        TelegramNotifier telegram,
        EmailNotifier email,
        IConfiguration config,
        ILogger<AlertCheckerService> logger)
    {
        _context = context;
        _telegram = telegram;
        _email = email;
        _config = config;
        _logger = logger;
    }

    public async Task RunAllChecksAsync()
    {
        _logger.LogInformation("🔍 Đang kiểm tra cảnh báo...");
        await CheckLowStockAsync();
        await CheckOverdueDebtAsync();
        await CheckDueSoonAsync();
        await CheckAbnormalTransactionsAsync();
        _logger.LogInformation("✅ Kiểm tra cảnh báo hoàn tất.");
    }

    // ── 1. Hàng sắp hết kho ──────────────────────────────────────────────
    private async Task CheckLowStockAsync()
    {
        // Join với BranchStock để lấy tồn kho từng chi nhánh
        // Dùng IgnoreQueryFilters để Background Service có thể thấy hết các chi nhánh
        var lowItems = await _context.BranchStocks
            .IgnoreQueryFilters()
            .Include(bs => bs.Product)
            .Include(bs => bs.Branch)
            .Where(bs => bs.Quantity <= bs.Product.MinStockThreshold)
            .ToListAsync();

        foreach (var bs in lowItems)
        {
            var key = $"LowStock-{bs.ProductId}-{bs.BranchId}";
            var isDup = await IsDuplicateAsync("LowStock", key);
            if (isDup) continue;

            var msg = $"⚠️ HÀNG SẮP HẾT KHO\nChi nhánh: {bs.Branch?.Name}\nSản phẩm: {bs.Product?.Name}\nTồn kho: {bs.Quantity} (ngưỡng: {bs.Product?.MinStockThreshold})\nCần nhập hàng ngay!";
            await CreateAndNotifyAsync("LowStock", "Warning", $"Hàng sắp hết [{bs.Branch?.Name}]: {bs.Product?.Name}", msg);
        }
    }

    // ── 2. Công nợ quá hạn > 30 ngày ─────────────────────────────────────
    private async Task CheckOverdueDebtAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(-30);
        var overdueInvoices = await _context.Invoices
            .Where(i => i.Status != "Paid" && i.DueDate < threshold)
            .ToListAsync();

        foreach (var inv in overdueInvoices)
        {
            var isDup = await IsDuplicateAsync("OverdueDebt", inv.InvoiceNumber);
            if (isDup) continue;

            var days = (int)(DateTime.UtcNow - inv.DueDate).TotalDays;
            var msg = $"🚨 CÔNG NỢ QUÁ HẠN\nHóa đơn: {inv.InvoiceNumber}\nKhách hàng: {inv.CustomerName}\nSố tiền: {inv.TotalAmount - inv.PaidAmount:N0}₫\nQuá hạn: {days} ngày";
            await CreateAndNotifyAsync("OverdueDebt", "Critical", $"Công nợ quá hạn: {inv.CustomerName}", msg);
        }
    }

    // ── 3. Hóa đơn sắp đến hạn (3 ngày tới) ─────────────────────────────
    private async Task CheckDueSoonAsync()
    {
        var soon = DateTime.UtcNow.AddDays(3);
        var dueSoon = await _context.Invoices
            .Where(i => i.Status != "Paid" && i.DueDate <= soon && i.DueDate >= DateTime.UtcNow)
            .ToListAsync();

        foreach (var inv in dueSoon)
        {
            var isDup = await IsDuplicateAsync("DueSoon", inv.InvoiceNumber);
            if (isDup) continue;

            var days = (int)(inv.DueDate - DateTime.UtcNow).TotalDays;
            var msg = $"⏰ SẮP ĐẾN HẠN THANH TOÁN\nHóa đơn: {inv.InvoiceNumber}\nKhách hàng: {inv.CustomerName}\nSố tiền: {inv.TotalAmount - inv.PaidAmount:N0}₫\nCòn: {days} ngày";
            await CreateAndNotifyAsync("DueSoon", "Warning", $"Sắp đến hạn: {inv.InvoiceNumber}", msg);
        }
    }

    // ── 4. Giao dịch bất thường ───────────────────────────────────────────
    private async Task CheckAbnormalTransactionsAsync()
    {
        var threshold = _config.GetValue<int>("Alerts:Thresholds:AbnormalQuantity", 100);
        var yesterday = DateTime.UtcNow.AddDays(-1);

        var abnormal = await _context.InvoiceItems
            .Include(i => i.Product)
            .Include(i => i.Invoice)
            .Where(i => i.Quantity > threshold && i!.Invoice!.InvoiceDate >= yesterday && i!.Invoice!.Status != "Paid")
            .ToListAsync();

        foreach (var item in abnormal)
        {
            var key = $"{item.InvoiceId}-{item.ProductId}";
            var isDup = await IsDuplicateAsync("AbnormalTx", key);
            if (isDup) continue;

            var msg = $"🚨 GIAO DỊCH BẤT THƯỜNG\nHóa đơn: {item.Invoice?.InvoiceNumber}\nSản phẩm: {item.Product?.Name}\nSố lượng: {item.Quantity} (ngưỡng: {threshold})\nCần kiểm tra lại!";
            await CreateAndNotifyAsync("AbnormalTx", "Critical", $"Số lượng bất thường: {item.Product?.Name}", msg);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private async Task<bool> IsDuplicateAsync(string type, string key)
    {
        var since = DateTime.UtcNow.AddHours(-6); // Không spam cùng 1 cảnh báo trong 6 giờ
        return await _context.AlertNotifications
            .AnyAsync(a => a.Type == type && a.Title.Contains(key) && a.CreatedAt >= since);
    }

    private async Task CreateAndNotifyAsync(string type, string severity, string title, string message)
    {
        // Lưu vào DB (in-app notification)
        var alert = new AlertNotification
        {
            Type = type,
            Severity = severity,
            Title = title,
            Message = message
        };
        _context.AlertNotifications.Add(alert);
        await _context.SaveChangesAsync();
        _logger.LogWarning("🔔 Alert mới: [{Severity}] {Title}", severity, title);

        // Gửi Telegram
        await _telegram.SendAsync(message);

        // Gửi Email
        await _email.SendAsync(title, message);
    }
}
