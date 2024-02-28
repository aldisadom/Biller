namespace Contracts.Requests.InvoiceClient;

public record InvoiceClientGetRequest
{
    public Guid? UserId { get; set; }
}
