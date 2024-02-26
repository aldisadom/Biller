namespace Application.Models;

public class ItemModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid? ShopId { get; set; }
}
