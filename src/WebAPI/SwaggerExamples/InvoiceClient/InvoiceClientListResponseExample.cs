using Contracts.Responses.InvoiceAddress;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceClient;

/// <summary>
/// example
/// </summary>
public class InvoiceAddressListResponseExample : IExamplesProvider<InvoiceAddressListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceAddressListResponse GetExamples()
    {
        InvoiceAddressListResponse clientList = new();

        clientList.InvoiceAddresss.Add(new InvoiceAddressResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "Glass Garden",
            Street = "Main road 5",
            City = "Winterfell",
            State = "Wolfswood",
            Email = "MainKeep@winter.com",
            Phone = "+123450679"
        });

        clientList.InvoiceAddresss.Add(new InvoiceAddressResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
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
