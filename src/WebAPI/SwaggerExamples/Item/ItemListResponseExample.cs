using BillerContracts.Enums;
using BillerContracts.Responses.Item;
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
        ItemListResponse itemListResponse = new()
        {
            TotalCount = 3,
            Page = 1,
            PageSize = PageSize.p25
        };

        itemListResponse.Items.Add(new ItemResponse()
        {
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m,
            Quantity = 1m,
            Version = 3
        });

        itemListResponse.Items.Add(new ItemResponse()
        {
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Lanister backstab",
            Price = 0.01m,
            Quantity = 1m,
            Version = 1
        });

        itemListResponse.Items.Add(new ItemResponse()
        {
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Hourly rate",
            Price = 10.00m,
            Quantity = 8m,
            Version = 2
        });

        return itemListResponse;
    }
}
