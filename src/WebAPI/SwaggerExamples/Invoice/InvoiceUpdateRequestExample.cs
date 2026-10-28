using BillerContracts.Requests.Invoice;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Invoice;

/// <summary>
/// example
/// </summary>
public class InvoiceUpdateRequestExample : IExamplesProvider<InvoiceUpdateRequest>
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
            SellerId = Guid.Parse("c85f22c7-1323-423a-a402-9b711a44c119"),
            CustomerId = Guid.Parse("b1f796e8-d23d-4b8b-b464-5e121e4385c7"),
            Items =
            [
                new()
                {
                    Id = Guid.Parse("f8ff867d-8e7f-4c4d-b7f3-0f7ac1ecfd43"),
                    Quantity = 2
                },
                new()
                {
                    Id = Guid.Parse("87b3b388-9e67-4a0d-b7f3-bcb4b458ab29"),
                    Quantity = 5,
                    Comments = "With defects"
                }
            ],
            Comments = "Invoice comment"
        };
    }
}
