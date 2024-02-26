using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.InvoiceItem;

public class InvoiceItemAddRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public decimal Price { get; set; }
    public Guid? ShopId { get; set; }
}
