using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Dtos;

public class ImportStockDto
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
    public int Quantity { get; set; }
    
    public string Note { get; set; } = string.Empty;
}
