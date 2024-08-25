namespace Contracts.Requests.Invoice;

public record InvoiceGetRequest
{
    public Guid? UserId { get; set; }
    public Guid? SellerId { get; set; }
    public Guid? CustomerId { get; set; }
}
