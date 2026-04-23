using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using ErpBackend.Data;
using ErpBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public class ExchangeRateService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly HttpClient _httpClient;

    public ExchangeRateService(AppDbContext context, IMemoryCache cache, ILogger<ExchangeRateService> logger, HttpClient httpClient)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<decimal> GetRateAsync(string currencyCode)
    {
        if (currencyCode == "VND") return 1.0m;

        var cacheKey = $"rate_{currencyCode}";
        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            return cachedRate;
        }

        // Try to get from DB for today
        var dbRate = await _context.ExchangeRates
            .Where(r => r.CurrencyCode == currencyCode && r.Date == DateTime.Today)
            .Select(r => r.Rate)
            .FirstOrDefaultAsync();

        if (dbRate > 0)
        {
            _cache.Set(cacheKey, dbRate, TimeSpan.FromHours(1));
            return dbRate;
        }

        // Fetch from API (Simplified mock or actual fetch)
        var fetchedRate = await FetchRateFromApi(currencyCode);
        
        // Save to DB
        _context.ExchangeRates.Add(new ExchangeRate
        {
            CurrencyCode = currencyCode,
            Rate = fetchedRate,
            Date = DateTime.Today
        });
        await _context.SaveChangesAsync();

        _cache.Set(cacheKey, fetchedRate, TimeSpan.FromHours(1));
        return fetchedRate;
    }

    private async Task<decimal> FetchRateFromApi(string currencyCode)
    {
        try
        {
            // For demo/Vibe: Using hardcoded logic with a bit of randomness to simulate live data
            // In production: Use HttpClient to call VCB/SBV API
            
            decimal baseRate = currencyCode switch
            {
                "USD" => 25450,
                "EUR" => 27120,
                "JPY" => 165,
                _ => 1.0m
            };

            // Add small random fluctuation
            var random = new Random();
            var fluctuation = (decimal)(random.NextDouble() * 10 - 5); // -5 to +5 VND
            return baseRate + fluctuation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy tỷ giá từ API");
            return 1.0m;
        }
    }
}
