using Contracts.Requests.InvoiceData;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataAddRequestExample : IExamplesProvider<InvoiceDataAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataAddRequest GetExamples()
    {
        return new InvoiceDataAddRequest()
        {
            UserId = Guid.Parse("2271c7b3-69ed-4e0c-a820-ecafbd57ac80"),
            SellerId = Guid.Parse("c85f22c7-1323-423a-a402-9b711a44c119"),
            CustomerId = Guid.Parse("b1f796e8-d23d-4b8b-b464-5e121e4385c7"),
            Items =
            [
                new()
                {
                    Id = Guid.Parse("f8ff867d-8e7f-4c4d-b7f3-0f7ac1ecfd43"),
                    Quantity = 1
                },
                new()
                {
                    Id = Guid.Parse("8c24bb2a-25f1-4d33-801e-1d79ff9e2958"),
                    Quantity = 3
                },
                new()
                {
                    Id = Guid.Parse("87b3b388-9e67-4a0d-b7f3-bcb4b458ab29"),
                    Quantity = 5,
                    Comments = "With defects"
                }
            ],
            Comments = "Not necessary",
            DueDate = DateTime.Now.AddDays(10)
        };
    }
}
