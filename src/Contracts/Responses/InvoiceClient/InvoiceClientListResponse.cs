namespace Contracts.Responses.InvoiceClient;

public record InvoiceClientListResponse
{
    public List<InvoiceClientResponse> InvoiceClients { get; set; } = [];
}
