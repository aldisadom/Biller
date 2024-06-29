using Contracts.Responses.InvoiceData;
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
        ItemListResponse itemListResponse = new();

        itemListResponse.Items.Add(new ItemResponse()
        {
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        });

        itemListResponse.Items.Add(new ItemResponse()
        {
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Lanister backstab",
            Price = 0.01m
        });

        itemListResponse.Items.Add(new ItemResponse()
        {
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Hourly rate",
            Price = 10.00m
        });

        return itemListResponse;
    }
}
