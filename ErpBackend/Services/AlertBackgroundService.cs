using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Services;

public class AlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertBackgroundService> _logger;
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public AlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Alert Background Service đã khởi động. Kiểm tra mỗi {Minutes} phút.", _interval.TotalMinutes);

        // Chạy lần đầu sau 10 giây (chờ DB sẵn sàng)
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var checker = scope.ServiceProvider.GetRequiredService<AlertCheckerService>();
                await checker.RunAllChecksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong Alert Background Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
