namespace Domain.Entities;

public record InvoiceDataEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int InvoiceNumber { get; set; }
    public string UserData { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime DueDate { get; set; }
    public string SellerData { get; set; } = string.Empty;
    public string CustomerData { get; set; } = string.Empty;
    public string ItemsData { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public decimal TotalPrice { get; set; }
}
