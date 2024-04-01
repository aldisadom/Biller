using Contracts.Requests.Item;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemUpdateRequestExample : IExamplesProvider<ItemUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public ItemUpdateRequest GetExamples()
    {
        return new ItemUpdateRequest()
        {
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        };
    }
}
