using Contracts.Requests.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceItem;

/// <summary>
/// example
/// </summary>
public class InvoiceItemAddRequestExample : IExamplesProvider<InvoiceItemAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemAddRequest GetExamples()
    {
        return new InvoiceItemAddRequest()
        {
            UserId = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        };
    }
}
