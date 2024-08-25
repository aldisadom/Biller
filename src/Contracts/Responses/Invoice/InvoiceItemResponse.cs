namespace Contracts.Responses.Invoice;

public class InvoiceItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Comments { get; set; } = string.Empty;
}
