namespace ErpBackend.Entities;

public class ExchangeRate
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty; // e.g., USD, EUR
    public decimal Rate { get; set; } // Relative to VND
    public DateTime Date { get; set; } = DateTime.Today;
}
