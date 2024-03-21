using Contracts.Requests.Customer;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Customer;

/// <summary>
/// example
/// </summary>
public class CustomerAddRequestExample : IExamplesProvider<CustomerAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public CustomerAddRequest GetExamples()
    {
        return new CustomerAddRequest()
        {
            SellerId = Guid.NewGuid(),
            CompanyName = "Glass Garden",
            Street = "Main road 5",
            City = "Winterfell",
            State = "Wolfswood",
            Email = "MainKeep@winter.com",
            Phone = "+123450679"
        };
    }
}
