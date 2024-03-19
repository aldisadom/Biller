namespace Contracts.Responses.Customer;

public record CustomerListResponse
{
    public List<CustomerResponse> Customers { get; set; } = [];
}
