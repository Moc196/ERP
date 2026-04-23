namespace ErpBackend.Entities;

public enum TransferStatus
{
    Pending,
    Completed,
    Cancelled
}

public class StockTransfer
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int FromBranchId { get; set; }
    public Branch FromBranch { get; set; } = null!;
    
    public int ToBranchId { get; set; }
    public Branch ToBranch { get; set; } = null!;
    
    public int Quantity { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
