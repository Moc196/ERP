using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Entities;

public class AccountType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Asset, Liability, Equity, Revenue, Expense

    [Required]
    [MaxLength(10)]
    public string NormalBalance { get; set; } = "Debit"; // Debit or Credit

    public List<Account> Accounts { get; set; } = new();
}
