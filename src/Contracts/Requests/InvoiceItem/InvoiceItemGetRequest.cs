namespace Contracts.Requests.InvoiceItem;

public record InvoiceItemGetRequest
{
    public Guid? AddressId { get; set; }
}
