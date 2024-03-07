namespace Contracts.Responses.InvoiceAddress;

public record InvoiceAddressListResponse
{
    public List<InvoiceAddressResponse> InvoiceAddresss { get; set; } = [];
}
