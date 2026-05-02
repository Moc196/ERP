using ErpBackend.Data;
using ErpBackend.Dtos;
using ErpBackend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Services.ICurrentUserService _currentUserService;
    private readonly Services.AccountingService _accountingService;

    public StockController(AppDbContext context, Services.ICurrentUserService currentUserService, Services.AccountingService accountingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _accountingService = accountingService;
    }

    [Authorize(Policy = "stock.import")]
    [HttpPost("import")]
    public async Task<IActionResult> ImportStock([FromBody] ImportStockDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Kể chuyện: Bắt đầu giao dịch nhập kho
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound(new { error = $"Không tìm thấy sản phẩm ID: {dto.ProductId}" });

            // 1. Tính toán giá vốn bình quân gia quyền (Weighted Average Cost)
            // Công thức: ((Tồn cũ * Giá vốn cũ) + (Số lượng mới * Giá nhập mới)) / Tổng tồn mới
            int currentTotalStock = await _context.BranchStocks.Where(bs => bs.ProductId == product.Id).SumAsync(bs => bs.Quantity);
            decimal totalValueBefore = currentTotalStock * product.CostPrice;
            decimal importValue = dto.Quantity * dto.PurchasePrice;
            
            product.CostPrice = (totalValueBefore + importValue) / (currentTotalStock + dto.Quantity);
            _context.Products.Update(product);

            // 2. Cập nhật tồn kho tại chi nhánh
            // Nếu không phải Admin tổng, bắt buộc dùng chi nhánh của User
            var branchId = _currentUserService.IsAdmin ? 1 : (_currentUserService.BranchId ?? 1); 
            
            var branchStock = await _context.BranchStocks.FirstOrDefaultAsync(bs => bs.ProductId == dto.ProductId && bs.BranchId == branchId);
            
            if (branchStock == null)
            {
                branchStock = new BranchStock { ProductId = dto.ProductId, BranchId = branchId, Quantity = dto.Quantity };
                _context.BranchStocks.Add(branchStock);
            }
            else
            {
                branchStock.Quantity += dto.Quantity;
            }

            // Ghi lại câu chuyện nhập kho
            var stockTx = new StockTransaction
            {
                ProductId = product.Id,
                BranchId = branchId,
                Quantity = dto.Quantity,
                Type = "Import",
                ReferenceId = string.IsNullOrWhiteSpace(dto.Note) ? "N/A" : dto.Note,
                CreatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown",
                CreatedAt = DateTime.UtcNow
            };

            _context.StockTransactions.Add(stockTx);
            await _context.SaveChangesAsync();

            // Tự động hạch toán Nhập kho (Nợ 156 / Có 331) - Dùng Giá nhập thực tế
            await _accountingService.AutoPostStockImportAsync(product, dto.Quantity, branchId, dto.PurchasePrice);
            await _context.SaveChangesAsync();

            // Chốt giao dịch
            await transaction.CommitAsync();

            return Ok(new 
            { 
                message = $"Đã nhập thêm {dto.Quantity} cho sản phẩm '{product.Name}' tại chi nhánh hiện tại.",
                newStock = branchStock.Quantity 
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int? threshold)
    {
        // Lấy danh sách sản phẩm có tồn kho bé hơn hoặc bằng MinStockThreshold
        // Join với BranchStock để lọc theo chi nhánh
        var branchId = _currentUserService.BranchId ?? 0;
        
        var lowStockProducts = await _context.Products
            .Where(p => p.BranchStocks.Any(bs => bs.BranchId == branchId && bs.Quantity <= (threshold ?? p.MinStockThreshold)))
            .Select(p => new 
            {
                p.Id,
                p.Name,
                Stock = p.BranchStocks.Where(bs => bs.BranchId == branchId).Select(bs => bs.Quantity).FirstOrDefault(),
                p.MinStockThreshold
            })
            .ToListAsync();

        return Ok(lowStockProducts);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetStockHistory([FromQuery] int? productId)
    {
        var query = _context.StockTransactions
            .Include(t => t.Product)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);

        var history = await query
            .Include(t => t.Branch)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                t.Id,
                t.Type,
                t.Quantity,
                t.ReferenceId,
                t.CreatedBy,
                t.CreatedAt,
                ProductName = t.Product!.Name,
                BranchName = t.Branch != null ? t.Branch.Name : "Hệ thống"
            })
            .Take(200)
            .ToListAsync();

        return Ok(history);
    }
}
