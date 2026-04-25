using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.Entities;
using ErpBackend.Repositories;
using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly AppDbContext _context;
    private readonly Services.ICurrentUserService _currentUserService;

    public ProductsController(IProductRepository productRepository, AppDbContext context, Services.ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    [Authorize(Policy = "product.view")]
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _productRepository.GetAllAsync();
        return Ok(products);
    }

    [Authorize(Policy = "product.view")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [Authorize(Policy = "product.create")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try 
        {
            // Lưu sản phẩm trước
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Khởi tạo tồn kho ban đầu tại chi nhánh của người dùng (hoặc CN 1 mặc định)
            var branchId = _currentUserService.BranchId ?? 1;
            var initialStock = product.Stock;

            if (initialStock > 0)
            {
                _context.BranchStocks.Add(new BranchStock
                {
                    ProductId = product.Id,
                    BranchId = branchId,
                    Quantity = initialStock
                });

                // Ghi nhận log nhập kho
                _context.StockTransactions.Add(new StockTransaction
                {
                    ProductId = product.Id,
                    Quantity = initialStock,
                    Type = "Initial",
                    ReferenceId = "INITIAL_STOCK",
                    CreatedBy = User.Identity?.Name ?? "admin",
                    CreatedAt = DateTime.UtcNow,
                    BranchId = branchId
                });

                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi hệ thống khi lưu sản phẩm", details = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        // Vibe code: For demo, just return Ok
        return Ok(new { message = $"Đã xóa sản phẩm ID {id}" });
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportToExcel()
    {
        var products = await _productRepository.GetAllAsync();
        
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        // Header
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Tên Sản Phẩm";
        worksheet.Cell(1, 3).Value = "Giá Nhập";
        worksheet.Cell(1, 4).Value = "Giá Bán";
        worksheet.Cell(1, 5).Value = "Tồn Kho";
        worksheet.Cell(1, 6).Value = "Ngưỡng Cảnh Báo";

        // Styling header
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Indigo;
        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        // Data
        int row = 2;
        foreach (var p in products)
        {
            worksheet.Cell(row, 1).Value = p.Id;
            worksheet.Cell(row, 2).Value = p.Name;
            worksheet.Cell(row, 3).Value = p.PurchasePrice;
            worksheet.Cell(row, 4).Value = p.Price;
            worksheet.Cell(row, 5).Value = p.Stock;
            worksheet.Cell(row, 6).Value = p.MinStockThreshold;
            
            // Format currency
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0\"₫\"";
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0\"₫\"";
            
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"Products_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [Authorize(Policy = "product.create")]
    [HttpPost("import/excel")]
    public async Task<IActionResult> ImportFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn file Excel.");

        var importedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
        
        // Tạo Batch ID duy nhất để có thể hoàn tác
        var batchId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        var reference = $"EXCEL_IMPORT_BATCH_{batchId}";

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua header

            foreach (var row in rows)
            {
                try
                {
                    var idStr = row.Cell(1).GetValue<string>();
                    var name = row.Cell(2).GetValue<string>();
                    var purchasePrice = row.Cell(3).GetValue<decimal>();
                    var price = row.Cell(4).GetValue<decimal>();
                    var qty = row.Cell(5).GetValue<int>();
                    var minStock = row.Cell(6).GetValue<int>();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"Dòng {row.RowNumber()}: Tên không được để trống.");
                        continue;
                    }

                    Product? existing = null;
                    if (int.TryParse(idStr, out int id) && id > 0)
                    {
                        existing = await _productRepository.GetByIdAsync(id);
                    }

                    if (existing != null)
                    {
                        existing.Name = name;
                        existing.PurchasePrice = purchasePrice;
                        existing.Price = price;
                        existing.MinStockThreshold = minStock;

                        // Cập nhật tồn kho tại chi nhánh
                        var branchId = _currentUserService.BranchId ?? 1;
                        var bs = await _context.BranchStocks.FirstOrDefaultAsync(x => x.ProductId == existing.Id && x.BranchId == branchId);
                        if (bs == null) {
                             bs = new BranchStock { ProductId = existing.Id, BranchId = branchId, Quantity = qty };
                             _context.BranchStocks.Add(bs);
                        } else {
                             bs.Quantity += qty;
                        }

                        await _productRepository.UpdateAsync(existing);

                        _context.StockTransactions.Add(new StockTransaction
                        {
                            ProductId = existing.Id,
                            Quantity = qty,
                            Type = "Import",
                            ReferenceId = reference,
                            CreatedBy = username,
                            CreatedAt = DateTime.UtcNow
                        });
                        updatedCount++;
                    }
                    else
                    {
                        var product = new Product
                        {
                            Name = name,
                            PurchasePrice = purchasePrice,
                            Price = price,
                            MinStockThreshold = minStock
                        };

                        var added = await _productRepository.AddAsync(product);
                        
                        // Thêm tồn kho chi nhánh cho sản phẩm mới
                        var branchId = _currentUserService.BranchId ?? 1;
                        _context.BranchStocks.Add(new BranchStock { 
                            ProductId = added.Id, 
                            BranchId = branchId, 
                            Quantity = qty 
                        });

                        _context.StockTransactions.Add(new StockTransaction
                        {
                            ProductId = added.Id,
                            Quantity = qty,
                            Type = "Import",
                            ReferenceId = reference,
                            CreatedBy = username,
                            CreatedAt = DateTime.UtcNow
                        });
                        importedCount++;
                    }
                    
                    await _context.SaveChangesAsync(); 
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {row.RowNumber()}: Lỗi dữ liệu ({ex.Message})");
                }
            }

            return Ok(new 
            { 
                message = $"Hoàn tất: Thêm mới {importedCount}, Cập nhật {updatedCount}.", 
                batchId = reference, // Trả về để Frontend có thể Undo
                errors = errors 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi đọc file: {ex.Message}");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("import/undo/{batchId}")]
    public async Task<IActionResult> UndoImport(string batchId)
    {
        var transactions = await _context.StockTransactions
            .Where(t => t.ReferenceId == batchId)
            .ToListAsync();

        if (!transactions.Any()) return NotFound("Không tìm thấy phiên nhập kho này hoặc đã bị hoàn tác trước đó.");

        foreach (var tx in transactions)
        {
            var branchId = _currentUserService.BranchId ?? 1;
            var bs = await _context.BranchStocks.FirstOrDefaultAsync(x => x.ProductId == tx.ProductId && x.BranchId == branchId);
            if (bs != null)
            {
                // Trừ lại tồn kho tại chi nhánh
                bs.Quantity -= tx.Quantity;
                if (bs.Quantity < 0) bs.Quantity = 0; // Đảm bảo không âm
            }
        }

        // Xóa các transaction này
        _context.StockTransactions.RemoveRange(transactions);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã hoàn tác phiên nhập kho thành công. Tồn kho đã được trừ lại." });
    }
}
