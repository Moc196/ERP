using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    
    [JsonIgnore]
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    
    // VD: Cash, Bank Transfer
    public string PaymentMethod { get; set; } = "Cash";
    public string ProcessedBy { get; set; } = "system";
}
