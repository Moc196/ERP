using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        
        public string? CustomerCode { get; set; } // Mã khách hàng (KH001...)
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxId { get; set; } // Mã số thuế
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<CustomerBranch> CustomerBranches { get; set; } = new List<CustomerBranch>();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<int>? BranchIds { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal TotalDebt { get; set; }
    }
}
