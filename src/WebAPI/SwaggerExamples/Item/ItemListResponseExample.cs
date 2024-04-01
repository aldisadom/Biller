using Contracts.Responses.Item;
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
            AddressId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        });

        ItemListResponse.Items.Add(new ItemResponse()
        {
            AddressId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Lanister backstab",
            Price = 0.01m
        });

        ItemListResponse.Items.Add(new ItemResponse()
        {
            AddressId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Hourly rate",
            Price = 10.00m
        });

        return ItemListResponse;
    }
}
