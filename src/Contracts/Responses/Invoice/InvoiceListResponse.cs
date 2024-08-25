namespace Contracts.Responses.Invoice;

public record InvoiceListResponse
{
    public List<InvoiceResponse> Invoices { get; set; } = [];
}
