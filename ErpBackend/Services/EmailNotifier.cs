using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Services;

public class EmailNotifier
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailNotifier> _logger;

    public EmailNotifier(IConfiguration config, ILogger<EmailNotifier> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string subject, string body)
    {
        var host     = _config["Alerts:Email:SmtpHost"] ?? "smtp.gmail.com";
        var port     = _config.GetValue<int>("Alerts:Email:SmtpPort", 587);
        var username = _config["Alerts:Email:Username"];
        var password = _config["Alerts:Email:Password"];
        var to       = _config["Alerts:Email:To"];

        // Bỏ qua nếu chưa cấu hình
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(to))
        {
            _logger.LogDebug("Email chưa cấu hình, bỏ qua.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(username));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = $"[ERP Alert] {subject}";
            message.Body = new TextPart("plain") { Text = body };

            using var smtp = new SmtpClient();

            // Gmail dùng STARTTLS trên port 587
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("📧 Email alert đã gửi đến {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Gửi email thất bại: {Message}", ex.Message);
        }
    }
}
