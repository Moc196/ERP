namespace ErpBackend.Dtos;

public class CreateInvoiceDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
