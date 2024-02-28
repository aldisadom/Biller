namespace Contracts.Requests.InvoiceItem;

public record InvoiceItemGetRequest
{
    public Guid? ClientId { get; set; }
}
