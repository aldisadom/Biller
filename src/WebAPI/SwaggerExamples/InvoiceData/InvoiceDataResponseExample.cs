using Contracts.Responses.Customer;
using Contracts.Responses.InvoiceData;
using Contracts.Responses.Item;
using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataResponseExample : IExamplesProvider<InvoiceDataResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataResponse GetExamples()
    {
        return new InvoiceDataResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Number = "0001",
            CreatedDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(10),
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
