using Contracts.Responses.Customer;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Customer;

/// <summary>
/// example
/// </summary>
public class CustomerListResponseExample : IExamplesProvider<CustomerListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public CustomerListResponse GetExamples()
    {
        CustomerListResponse clientList = new();

        clientList.Customers.Add(new CustomerResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "Glass Garden",
            Street = "Main road 5",
            City = "Winterfell",
            State = "Wolfswood",
            Email = "MainKeep@winter.com",
            Phone = "+123450679"
        });

        clientList.Customers.Add(new CustomerResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "Throne Takers",
            Street = "Ocean road 1",
            City = "Casterly Rock",
            State = "Westerlands",
            Email = "IronThrone@backstab.com",
            Phone = "+9623330679"
        });

        return clientList;
    }
}
