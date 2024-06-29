namespace Contracts.Requests.InvoiceData;

public record InvoiceDataGetRequest
{
    public Guid? UserId { get; set; }
    public Guid? SellerId { get; set; }
    public Guid? CustomerId { get; set; }
}
