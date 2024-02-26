using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests;

public class ItemUpdateRequest
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public decimal Price { get; set; }
    public Guid? ShopId { get; set; }
}
