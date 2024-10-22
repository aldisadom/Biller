namespace Contracts.Requests.Invoice;

public class InvoiceItemRequest
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }
    public string Comments { get; set; } = string.Empty;
}
