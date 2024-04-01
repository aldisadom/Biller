namespace Application.Models;

public class ItemModel
{
    public Guid Id { get; set; }
    public Guid AddressId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
