namespace Contracts.Requests.Item;

public record ItemAddRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Quantity { get; set; }
}
