using System.ComponentModel.DataAnnotations;

namespace Contracts.Responses.Customer;

public record CustomerResponse
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string InvoiceName { get; set; } = string.Empty;
    public string CompanyNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
