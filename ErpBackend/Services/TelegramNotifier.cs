using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Services;

public class TelegramNotifier
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<TelegramNotifier> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(string message)
    {
        var token = _config["Alerts:Telegram:BotToken"];
        var chatId = _config["Alerts:Telegram:ChatId"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            _logger.LogDebug("Telegram chưa cấu hình, bỏ qua.");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new { chat_id = chatId, text = message };
            var response = await client.PostAsJsonAsync(url, payload);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("📱 Telegram alert đã gửi");
            else
                _logger.LogWarning("⚠️ Telegram gửi thất bại: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi gửi Telegram");
        }
    }
}
