using Contracts.Requests.InvoiceAddress;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceClient;

/// <summary>
/// example
/// </summary>
public class InvoiceAddressUpdateRequestExample : IExamplesProvider<InvoiceAddressUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceAddressUpdateRequest GetExamples()
    {
        return new InvoiceAddressUpdateRequest()
        {
            Id = Guid.NewGuid(),
            CompanyName = "Throne Takers",
            Street = "Ocean road 1",
            City = "Casterly Rock",
            State = "Westerlands",
            Email = "IronThrone@backstab.com",
            Phone = "+9623330679"
        };
    }
}

