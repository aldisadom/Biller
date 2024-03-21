using Contracts.Responses.Customer;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataGenerateResponseExample : IExamplesProvider<CustomerResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public CustomerResponse GetExamples()
    {
        return new CustomerResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "Throne Takers",
            Street = "Ocean road 1",
            City = "Casterly Rock",
            State = "Westerlands",
            Email = "IronThrone@backstab.com",
            Phone = "+9623330679"
        };
    }
}

