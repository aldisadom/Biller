using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.Customer;

public record CustomerAddRequest
{
    [Required]
    public Guid SellerId { get; set; }
    [Required]
    public string InvoiceName { get; set; } = string.Empty;
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    [Required]
    public string Street { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Phone { get; set; } = string.Empty;
}
