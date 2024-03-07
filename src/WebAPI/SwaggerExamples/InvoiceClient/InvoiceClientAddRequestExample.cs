using Contracts.Requests.InvoiceAddress;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceClient;

/// <summary>
/// example
/// </summary>
public class InvoiceAddressAddRequestExample : IExamplesProvider<InvoiceAddressAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceAddressAddRequest GetExamples()
    {
        return new InvoiceAddressAddRequest()
        {
            UserId = Guid.NewGuid(),
            CompanyName = "Glass Garden",
            Street = "Main road 5",
            City = "Winterfell",
            State = "Wolfswood",
            Email = "MainKeep@winter.com",
            Phone = "+123450679"
        };
    }
}
