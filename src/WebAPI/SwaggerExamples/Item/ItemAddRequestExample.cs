using Contracts.Requests;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemAddRequestExample : IExamplesProvider<ItemAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public ItemAddRequest GetExamples()
    {
        return new ItemAddRequest()
        {
            Name = "Toy car",
            Price = 5.11m,
            ShopId = Guid.Parse("6a1e7354-e67f-4795-a174-4aacd51bacc3")
        };
    }
}
