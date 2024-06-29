using Contracts.Requests.InvoiceData;
using Contracts.Requests.Item;
using Contracts.Responses.Customer;
using Contracts.Responses.InvoiceData;
using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataUpdateRequestExample : IExamplesProvider<InvoiceDataUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataUpdateRequest GetExamples()
    {
        return new InvoiceDataUpdateRequest()
        {
            Id = Guid.NewGuid(),            
            CreatedDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(10),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
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
