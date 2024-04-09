using Contracts.Responses.InvoiceData;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataAddResponseExample : IExamplesProvider<InvoiceDataAddResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataAddResponse GetExamples()
    {
        return new InvoiceDataAddResponse()
        {
            Id = new Guid()
        };
    }
}

