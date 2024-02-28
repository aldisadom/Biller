using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.InvoiceClient;

public record InvoiceClientAddRequest
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    [Required]
    public string Street { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public object Email { get; set; } = string.Empty;
    [Required]
    public string Phone { get; set; } = string.Empty;
    [Required]
    public Guid UserId { get; set; }
}
