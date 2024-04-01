using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.Item;

public record ItemUpdateRequest
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public decimal Price { get; set; }
    [Required]
    public int Quantity { get; set; }
}
