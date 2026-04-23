using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    
    // Multi-currency
    public string CurrencyCode { get; set; } = "VND";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal TotalAmountVND { get; set; } // Store converted amount for reports
    
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public string Status { get; set; } = "Unpaid";
    public string CreatedBy { get; set; } = "system";
    public int? BranchId { get; set; } // For RLS

    public List<InvoiceItem> Items { get; set; } = new();
}
