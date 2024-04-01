using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public record SellerEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public object Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankNumber { get; set; } = string.Empty;
}
