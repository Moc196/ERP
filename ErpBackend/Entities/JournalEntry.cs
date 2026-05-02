using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Entities;

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DateTime EntryDate { get; set; }

    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty; // e.g., INV-001

    public string Description { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<JournalEntryLine> Lines { get; set; } = new();
}
