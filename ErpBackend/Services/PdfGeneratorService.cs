using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ErpBackend.Services;

public class PdfGeneratorService
{
    private string? _executablePath;

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent)
    {
        if (_executablePath == null)
        {
            var downloadPath = Path.Combine(Directory.GetCurrentDirectory(), ".local-chromium");
            if (!Directory.Exists(downloadPath)) Directory.CreateDirectory(downloadPath);

            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
            {
                Path = downloadPath
            });
            
            var result = await browserFetcher.DownloadAsync();
            _executablePath = result.GetExecutablePath();
        }

        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            ExecutablePath = _executablePath,
            Headless = true,
            Args = new[] { 
                "--no-sandbox", 
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu"
            },
            DumpIO = true
        });

        using var page = await browser.NewPageAsync();
        await page.SetContentAsync(htmlContent);
        
        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "20px",
                Right = "20px",
                Bottom = "20px",
                Left = "20px"
            }
        });
    }
}
