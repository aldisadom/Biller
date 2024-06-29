using Contracts.Responses.Item;

namespace Contracts.Responses.InvoiceData;

public record InvoiceDataListResponse
{
    public List<InvoiceDataResponse> Invoices { get; set; } = [];
}
