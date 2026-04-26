using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class Product
{
    public int Id { get; set; }
    [Required]
    public string ProductCode { get; set; } = string.Empty; // Mã SKU (SP001...)
    public string Name { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; } // Giá nhập
    public decimal Price { get; set; } // Giá bán
    public int MinStockThreshold { get; set; } = 5;

    private int? _initialStock;
    [NotMapped]
    [JsonPropertyName("stock")]
    public int Stock 
    { 
        get => (BranchStocks != null && BranchStocks.Any()) ? BranchStocks.Sum(s => s.Quantity) : (_initialStock ?? 0);
        set => _initialStock = value;
    }
    
    [JsonPropertyName("branchStocks")]
    public List<BranchStock> BranchStocks { get; set; } = new();
}
