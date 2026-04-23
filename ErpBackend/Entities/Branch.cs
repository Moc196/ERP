using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    [JsonIgnore]
    public List<BranchStock> BranchStocks { get; set; } = new();
}
