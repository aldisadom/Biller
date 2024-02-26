using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.InvoiceItem;

public class InvoiceItemUpdateRequest
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public decimal Price { get; set; }
}
