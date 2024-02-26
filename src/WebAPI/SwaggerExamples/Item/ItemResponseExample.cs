using Contracts.Responses.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemResponseExample : IExamplesProvider<InvoiceItemResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemResponse GetExamples()
    {
        return new InvoiceItemResponse()
        {
            Id = Guid.Parse("51427c65-fb49-42be-a651-a0a1dee84931"),
            Name = "Toy car",
            Price = 5.11m
        };
    }
}
