using Contracts.Responses;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemAddResponseExample : IExamplesProvider<ItemAddResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public ItemAddResponse GetExamples()
    {
        return new ItemAddResponse()
        {
            Id = Guid.Parse("51427c65-fb49-42be-a651-a0a1dee84931")
        };
    }
}
