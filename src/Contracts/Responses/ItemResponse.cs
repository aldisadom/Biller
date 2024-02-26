namespace Contracts.Responses;

public class ItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid? ShopId { get; set; }
}
