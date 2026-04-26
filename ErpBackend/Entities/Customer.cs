using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        public string CustomerCode { get; set; } = string.Empty; // Mã khách hàng (KH001...)
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxId { get; set; } // Mã số thuế
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int? BranchId { get; set; } // Để phân vùng khách hàng theo chi nhánh
        public virtual Branch? Branch { get; set; }
    }
}
