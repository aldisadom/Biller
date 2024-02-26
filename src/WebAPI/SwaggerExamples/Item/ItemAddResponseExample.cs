using Contracts.Responses.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemAddResponseExample : IExamplesProvider<InvoiceItemAddResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemAddResponse GetExamples()
    {
        return new InvoiceItemAddResponse()
        {
            Id = Guid.Parse("51427c65-fb49-42be-a651-a0a1dee84931")
        };
    }
}
