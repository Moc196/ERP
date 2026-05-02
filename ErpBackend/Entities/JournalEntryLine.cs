using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Entities;

public class JournalEntryLine
{
    public long Id { get; set; }

    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Debit { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Credit { get; set; } = 0;
}
