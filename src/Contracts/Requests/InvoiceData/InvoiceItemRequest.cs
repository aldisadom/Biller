using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.InvoiceData;

public class InvoiceItemRequest
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public int Quantity { get; set; }
    public string Comments { get; set; } = string.Empty;
}
