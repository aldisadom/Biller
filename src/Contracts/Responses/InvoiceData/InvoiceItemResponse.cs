namespace Contracts.Responses.InvoiceData;

public class InvoiceItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Comments { get; set; } = string.Empty;
}
