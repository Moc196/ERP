using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Dtos;

public class PaymentDto
{
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
    public decimal Amount { get; set; }
    
    public string PaymentMethod { get; set; } = "Cash";
}
