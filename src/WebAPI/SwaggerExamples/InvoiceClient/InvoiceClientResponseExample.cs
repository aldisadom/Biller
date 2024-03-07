using Contracts.Responses.InvoiceAddress;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceClient;

/// <summary>
/// example
/// </summary>
public class InvoiceAddressResponseExample : IExamplesProvider<InvoiceAddressResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceAddressResponse GetExamples()
    {
        return new InvoiceAddressResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "Throne Takers",
            Street = "Ocean road 1",
            City = "Casterly Rock",
            State = "Westerlands",
            Email = "IronThrone@backstab.com",
            Phone = "+9623330679"
        };
    }
}
