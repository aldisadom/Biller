namespace Contracts.Requests.Customer;

public record CustomerGetRequest
{
    public Guid? UserId { get; set; }
}
