namespace Contracts.Requests.Invoice;

public class InvoiceItemUpdateRequest
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Comments { get; set; } = string.Empty;
}
