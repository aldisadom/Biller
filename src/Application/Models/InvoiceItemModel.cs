namespace Application.Models;

public class InvoiceItemModel
{
    public Guid Id { get; set; }
    public Guid AddressId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
