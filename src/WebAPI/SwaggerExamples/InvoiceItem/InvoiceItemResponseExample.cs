using Contracts.Responses.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceItem;

/// <summary>
/// example
/// </summary>
public class InvoiceItemResponseExample : IExamplesProvider<InvoiceItemResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemResponse GetExamples()
    {
        return new InvoiceItemResponse()
        {
            AddressId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        };
    }
}
