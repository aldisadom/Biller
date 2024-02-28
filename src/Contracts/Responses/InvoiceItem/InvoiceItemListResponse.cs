namespace Contracts.Responses.InvoiceItem;

public record InvoiceItemListResponse
{
    public List<InvoiceItemResponse> InvoiceItems { get; set; } = [];
}
