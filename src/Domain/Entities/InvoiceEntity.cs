
using BillerContracts.Enums;

namespace Domain.Entities;

public record InvoiceEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public Guid SeriesId { get; set; }
    public string SeriesName { get; set; } = string.Empty;
    public int InvoiceNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime DueDate { get; set; }
    public string ItemsData { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public decimal TotalPrice { get; set; }
    public InvoiceStatus Status { get; set; }
}
