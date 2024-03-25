using Contracts.Requests.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Seller;

/// <summary>
/// example
/// </summary>
public class SellerUpdateRequestExample : IExamplesProvider<SellerUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SellerUpdateRequest GetExamples()
    {
        return new SellerUpdateRequest()
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

