using Contracts.Requests.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceItem;

/// <summary>
/// example
/// </summary>
public class InvoiceItemUpdateRequestExample : IExamplesProvider<InvoiceItemUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemUpdateRequest GetExamples()
    {
        return new InvoiceItemUpdateRequest()
        {
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        };
    }
}
