using Contracts.Requests.InvoiceClient;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceClient;

/// <summary>
/// example
/// </summary>
public class InvoiceClientUpdateRequestExample : IExamplesProvider<InvoiceClientUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceClientUpdateRequest GetExamples()
    {
        return new InvoiceClientUpdateRequest()
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

