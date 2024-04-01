using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.Item;

public record ItemAddRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public decimal Price { get; set; }
    [Required]
    public Guid AddressId { get; set; }
    [Required]
    public int Quantity { get; set; }
}
