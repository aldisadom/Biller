namespace Contracts.Requests.Item;

public record ItemGetRequest
{
    public Guid? AddressId { get; set; }
}
