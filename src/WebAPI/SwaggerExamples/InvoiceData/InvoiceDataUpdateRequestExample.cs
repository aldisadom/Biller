using Contracts.Requests.Customer;
using Contracts.Requests.InvoiceData;
using Contracts.Requests.Seller;
using Swashbuckle.AspNetCore.Filters;

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
            UserId = Guid.NewGuid(),
            Seller = new SellerUpdateRequest()
            {
                Id = Guid.NewGuid()
            },
            Customer = new CustomerUpdateRequest()
            {
                Id = Guid.NewGuid()
            },
            Items =
            [
                new InvoiceItemUpdateRequest()
                {
                    Id = Guid.NewGuid(),
                }
            ],
            Comments = "Invoice comment"
        };
    }
}
