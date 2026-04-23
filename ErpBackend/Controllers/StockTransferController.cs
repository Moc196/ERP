using ErpBackend.Data;
using ErpBackend.Entities;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockTransferController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public StockTransferController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> TransferStock([FromBody] StockTransferRequest request)
    {
        if (request.Quantity <= 0) return BadRequest("Số lượng phải lớn hơn 0.");
        if (request.FromBranchId == request.ToBranchId) return BadRequest("Chi nhánh nguồn và đích phải khác nhau.");

        // Bảo mật: Nếu không phải Admin tổng, chỉ được phép chuyển hàng TỪ chi nhánh của mình
        if (!_currentUserService.IsAdmin && request.FromBranchId != _currentUserService.BranchId)
        {
            return Forbid();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra tồn kho chi nhánh nguồn
            var fromStock = await _context.BranchStocks
                .FirstOrDefaultAsync(bs => bs.ProductId == request.ProductId && bs.BranchId == request.FromBranchId);
            
            if (fromStock == null || fromStock.Quantity < request.Quantity)
                return BadRequest("Chi nhánh nguồn không đủ tồn kho.");

            // 2. Cập nhật tồn kho chi nhánh đích
            var toStock = await _context.BranchStocks
                .FirstOrDefaultAsync(bs => bs.ProductId == request.ProductId && bs.BranchId == request.ToBranchId);
            
            if (toStock == null)
            {
                toStock = new BranchStock { ProductId = request.ProductId, BranchId = request.ToBranchId, Quantity = request.Quantity };
                _context.BranchStocks.Add(toStock);
            }
            else
            {
                toStock.Quantity += request.Quantity;
            }

            // 3. Trừ tồn kho chi nhánh nguồn
            fromStock.Quantity -= request.Quantity;

            // 4. Lưu lịch sử điều chuyển
            var transfer = new StockTransfer
            {
                ProductId = request.ProductId,
                FromBranchId = request.FromBranchId,
                ToBranchId = request.ToBranchId,
                Quantity = request.Quantity,
                Status = TransferStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };
            _context.StockTransfers.Add(transfer);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Điều chuyển hàng hóa thành công." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetTransferHistory()
    {
        var history = await _context.StockTransfers
            .Include(t => t.Product)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                ProductName = t.Product.Name,
                FromBranchName = t.FromBranch.Name,
                ToBranchName = t.ToBranch.Name,
                t.Quantity,
                t.Status,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(history);
    }
}

public class StockTransferRequest
{
    public int ProductId { get; set; }
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public int Quantity { get; set; }
}
