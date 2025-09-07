using Common.Enums;
using Contracts.Requests.Invoice;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Invoice;

/// <summary>
/// example
/// </summary>
public class InvoiceUpdateStatusRequestExample : IExamplesProvider<InvoiceUpdateStatusRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceUpdateStatusRequest GetExamples()
    {
        return new InvoiceUpdateStatusRequest()
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Send
        };
    }
}
