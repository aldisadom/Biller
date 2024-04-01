using Contracts.Requests.Item;
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
            AddressId = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        };
    }
}
