using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Seller;

/// <summary>
/// example
/// </summary>
public class SellerListResponseExample : IExamplesProvider<SellerListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SellerListResponse GetExamples()
    {
        SellerListResponse clientList = new();

        clientList.Sellers.Add(new SellerResponse()
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

        clientList.Sellers.Add(new SellerResponse()
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
