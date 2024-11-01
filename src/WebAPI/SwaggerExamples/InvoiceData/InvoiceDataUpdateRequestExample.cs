using Contracts.Requests.Customer;
using Contracts.Requests.Invoice;
using Contracts.Requests.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataUpdateRequestExample : IExamplesProvider<InvoiceUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceUpdateRequest GetExamples()
    {
        return new InvoiceUpdateRequest()
        {
            Id = Guid.NewGuid(),
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
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
