using Contracts.Responses.Customer;
using Contracts.Responses.Invoice;
using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataResponseExample : IExamplesProvider<InvoiceResponse>
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
            InvoiceNumber = 1,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
            Seller = new SellerResponse
            {
                Id = Guid.NewGuid(),
            },
            Customer = new CustomerResponse
            {
                Id = Guid.NewGuid(),
            },
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
