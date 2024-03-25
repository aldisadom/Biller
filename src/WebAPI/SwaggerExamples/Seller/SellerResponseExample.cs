using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Seller;

/// <summary>
/// example
/// </summary>
public class SellerResponseExample : IExamplesProvider<SellerResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SellerResponse GetExamples()
    {
        return new SellerResponse()
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
