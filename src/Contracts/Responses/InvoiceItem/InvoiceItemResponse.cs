namespace Contracts.Responses.InvoiceItem;

public record InvoiceItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
