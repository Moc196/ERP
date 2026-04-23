namespace ErpBackend.Entities;

public class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int Quantity { get; set; }
    
    // "Import" or "Export"
    public string Type { get; set; } = string.Empty;
    
    // InvoiceId or any reference string like "Import-001"
    public string ReferenceId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
