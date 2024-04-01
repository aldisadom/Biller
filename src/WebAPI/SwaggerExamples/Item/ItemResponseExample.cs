using Contracts.Responses.Item;
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
            CustomerId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        };
    }
}
