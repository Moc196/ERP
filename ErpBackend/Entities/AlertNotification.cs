namespace ErpBackend.Entities;

public class AlertNotification
{
    public int Id { get; set; }

    // "LowStock" | "OverdueDebt" | "DueSoon" | "LowProfit" | "AbnormalTx"
    public string Type { get; set; } = string.Empty;

    // "Warning" | "Critical"
    public string Severity { get; set; } = "Warning";

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
