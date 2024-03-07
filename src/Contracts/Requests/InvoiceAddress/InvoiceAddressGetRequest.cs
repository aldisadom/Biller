namespace Contracts.Requests.InvoiceAddress;

public record InvoiceAddressGetRequest
{
    public Guid? UserId { get; set; }
}
