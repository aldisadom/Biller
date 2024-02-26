namespace Contracts.Responses.InvoiceItem;

public class InvoiceItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
