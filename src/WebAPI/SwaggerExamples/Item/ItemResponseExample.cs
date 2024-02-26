using Contracts.Responses;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemResponseExample : IExamplesProvider<ItemResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public ItemResponse GetExamples()
    {
        return new ItemResponse()
        {
            Id = Guid.Parse("51427c65-fb49-42be-a651-a0a1dee84931"),
            Name = "Toy car",
            Price = 5.11m,
            ShopId = Guid.Parse("6a1e7354-e67f-4795-a174-4aacd51bacc3")
        };
    }
}
