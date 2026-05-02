using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ErpBackend.Entities;

public class Account
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty; // e.g., 111, 1121

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int AccountTypeId { get; set; }
    public AccountType AccountType { get; set; } = null!;

    public int? ParentId { get; set; }
    
    [JsonIgnore]
    public Account? Parent { get; set; }
    
    public List<Account> Children { get; set; } = new();

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public List<JournalEntryLine> JournalEntryLines { get; set; } = new();
}
