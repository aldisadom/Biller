using Contracts.Responses;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemListResponseExample : IExamplesProvider<ItemListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public ItemListResponse GetExamples()
    {
        ItemListResponse ItemListResponse = new();

        ItemListResponse.Items.Add(new ItemResponse()
        {
            Id = Guid.Parse("51427c65-fb49-42be-a651-a0a1dee84931"),
            Name = "Toy car",
            Price = 5.11m,
            ShopId = Guid.Parse("6a1e7354-e67f-4795-a174-4aacd51bacc3")
        });

        ItemListResponse.Items.Add(new ItemResponse()
        {
            Id = Guid.Parse("d4ec61ea-b5d3-4967-aa76-23f3990de955"),
            Name = "Teddy bear",
            Price = 15.99m,
            ShopId = Guid.Parse("6a1e7354-e67f-4795-a174-4aacd51bacc3")
        });

        return ItemListResponse;
    }
}
