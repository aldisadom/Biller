namespace Contracts.Requests.Customer;

public record CustomerGetRequest
{
    public Guid? SellerId { get; set; }
}
