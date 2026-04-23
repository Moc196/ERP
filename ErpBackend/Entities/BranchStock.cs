using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class BranchStock
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }
    [JsonIgnore]
    public Product Product { get; set; } = null!;
    
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }
    [JsonPropertyName("branch")]
    public Branch Branch { get; set; } = null!;
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
