using BillerContracts.Responses.Invoice;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Invoice;

/// <summary>
/// example
/// </summary>
public class InvoiceResponseExample : IExamplesProvider<InvoiceResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceResponse GetExamples()
    {
        return new InvoiceResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesName = "WF",
            InvoiceNumber = 1,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
            SellerId = Guid.NewGuid(),
            SellerName = "Throne Takers",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Winterfell",
            Items =
            [
                new InvoiceItemResponse()
                {
                    Id = Guid.NewGuid(),
                }
            ],
            Comments = "Invoice comment",
            TotalPrice = 1500m,
        };
    }
}
