using Contracts.Requests.Customer;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Customer;

/// <summary>
/// example
/// </summary>
public class CustomerUpdateRequestExample : IExamplesProvider<CustomerUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public CustomerUpdateRequest GetExamples()
    {
        return new CustomerUpdateRequest()
        {
            Id = Guid.NewGuid(),
            InvoiceName = "TT",
            CompanyName = "Throne Takers",
            CompanyNumber = "TT896552",
            Street = "Ocean road 1",
            City = "Casterly Rock",
            State = "Westerlands",
            Email = "IronThrone@backstab.com",
            Phone = "+9623330679"
        };
    }
}

