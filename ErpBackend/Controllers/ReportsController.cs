using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "report.export")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
        // EPPlus v8+ dùng cách mới này thay vì LicenseContext cũ (đã deprecated)
        ExcelPackage.License.SetNonCommercialPersonal("ERP");
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
        var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1); // hết ngày 23:59:59.999

        var invoices = await _context.Invoices
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate && i.Status == "Paid")
            .ToListAsync();

        var totalRevenue = invoices.Sum(i => i.TotalAmountVND);
        var totalInvoices = invoices.Count;
        var totalDays = (endDate - startDate).TotalDays;
        totalDays = totalDays <= 0 ? 1 : totalDays;

        var dailyBreakdown = invoices
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Revenue = g.Sum(i => i.TotalAmountVND),
                InvoiceCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(new
        {
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            TotalRevenue = totalRevenue,
            TotalInvoices = totalInvoices,
            AveragePerDay = Math.Round((decimal)(totalRevenue / (decimal)totalDays), 2),
            DailyBreakdown = dailyBreakdown
        });
    }

    [HttpGet("profit")]
    public async Task<IActionResult> GetProfitReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
        var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var invoiceItems = await _context.InvoiceItems
            .Include(item => item.Invoice)
            .Include(item => item.Product)
            .Where(item => item.Invoice != null && item.Invoice.InvoiceDate >= startDate && item.Invoice.InvoiceDate <= endDate && item.Invoice.Status == "Paid")
            .ToListAsync();

        var totalRevenue = invoiceItems.Sum(i => i.Quantity * i.UnitPrice);
        // Profit = (Giá Bán - Giá Nhập) * Số lượng
        var totalProfit = invoiceItems.Sum(i => (i.UnitPrice - (i.Product?.PurchasePrice ?? 0)) * i.Quantity);

        var profitByProduct = invoiceItems
            .GroupBy(i => new { i.ProductId, i.Product?.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice),
                Profit = g.Sum(i => (i.UnitPrice - (i.Product?.PurchasePrice ?? 0)) * i.Quantity)
            })
            .OrderByDescending(x => x.Profit)
            .ToList();

        return Ok(new
        {
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            TotalRevenue = totalRevenue,
            TotalProfit = totalProfit,
            ProfitMargin = totalRevenue == 0 ? 0 : Math.Round((totalProfit / totalRevenue) * 100, 2),
            ProfitByProduct = profitByProduct
        });
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
    {
        var topProducts = await _context.InvoiceItems
            .Where(i => i.Invoice != null && i.Invoice.Status == "Paid")
            .GroupBy(i => new { i.ProductId, i.Product.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalQuantitySold = g.Sum(i => i.Quantity),
                TotalRevenueGenerated = g.Sum(i => i.Quantity * i.UnitPrice)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(limit)
            .ToListAsync();

        return Ok(topProducts);
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] string type = "revenue")
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Report");

        if (type.ToLower() == "revenue")
        {
            var invoices = await _context.Invoices
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            // Header
            worksheet.Cells[1, 1].Value = "BÁO CÁO DOANH THU";
            worksheet.Cells[1, 1, 1, 5].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[1, 1].Style.Font.Color.SetColor(Color.DarkBlue);

            worksheet.Cells[3, 1].Value = "Mã Hóa Đơn";
            worksheet.Cells[3, 2].Value = "Khách Hàng";
            worksheet.Cells[3, 3].Value = "Ngày Bán";
            worksheet.Cells[3, 4].Value = "Tổng Tiền";
            worksheet.Cells[3, 5].Value = "Đã Trả";
            worksheet.Cells[3, 6].Value = "Còn Nợ";
            worksheet.Cells[3, 7].Value = "Hạn TT";
            worksheet.Cells[3, 8].Value = "Trạng Thái";

            using (var range = worksheet.Cells[3, 1, 3, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(63, 81, 181)); // Indigo
                range.Style.Font.Color.SetColor(Color.White);
            }

            // Data
            int row = 4;
            foreach (var inv in invoices)
            {
                worksheet.Cells[row, 1].Value = inv.InvoiceNumber;
                worksheet.Cells[row, 2].Value = inv.CustomerName;
                worksheet.Cells[row, 3].Value = inv.InvoiceDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 4].Value = (double)inv.TotalAmount;
                worksheet.Cells[row, 5].Value = (double)inv.PaidAmount;
                worksheet.Cells[row, 6].Value = (double)(inv.TotalAmount - inv.PaidAmount);
                worksheet.Cells[row, 7].Value = inv.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = inv.Status;

                // Format tiền tệ
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";

                // Màu dòng xen kẽ
                if (row % 2 == 0)
                {
                    using var rowRange = worksheet.Cells[row, 1, row, 8];
                    rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 242, 255));
                }

                // Tô đỏ nếu quá hạn chưa trả
                if (inv.Status != "Paid" && inv.DueDate < DateTime.UtcNow)
                {
                    worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 8].Style.Font.Bold = true;
                }

                row++;
            }

            // Tổng kết cuối bảng
            worksheet.Cells[row + 1, 3].Value = "TỔNG CỘNG:";
            worksheet.Cells[row + 1, 3].Style.Font.Bold = true;
            worksheet.Cells[row + 1, 4].Formula = $"SUM(D4:D{row - 1})";
            worksheet.Cells[row + 1, 5].Formula = $"SUM(E4:E{row - 1})";
            worksheet.Cells[row + 1, 6].Formula = $"SUM(F4:F{row - 1})";
            worksheet.Cells[row + 1, 4, row + 1, 6].Style.Numberformat.Format = "#,##0";
            worksheet.Cells[row + 1, 4, row + 1, 6].Style.Font.Bold = true;
            worksheet.Cells[row + 1, 4, row + 1, 6].Style.Font.Color.SetColor(Color.DarkBlue);

            // AutoFitColumns chỉ gọi khi có data (tránh NullRef khi invoice rỗng)
            if (worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
        else
        {
            return BadRequest(new { error = "Chỉ hỗ trợ export type=revenue hiện tại." });
        }

        var stream = new MemoryStream();
        await package.SaveAsAsync(stream);
        stream.Position = 0;

        string excelName = $"Report_{type}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
    }
}
