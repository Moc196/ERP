using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; } // Giá nhập
    public decimal Price { get; set; } // Giá bán
    public int MinStockThreshold { get; set; } = 5;

    [NotMapped]
    [JsonPropertyName("stock")]
    public int Stock => BranchStocks?.Sum(s => s.Quantity) ?? 0;
    
    [JsonPropertyName("branchStocks")]
    public List<BranchStock> BranchStocks { get; set; } = new();
}
