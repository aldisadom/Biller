using Contracts.Responses.Customer;
using Contracts.Responses.InvoiceData;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataGenerateResponseExample : IExamplesProvider<InvoiceDataGenerateResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataGenerateResponse GetExamples()
    {
        return new InvoiceDataGenerateResponse()
        {
            FileName = "50_invoice.pdf"
        };
    }
}

