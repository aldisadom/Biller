using Contracts.Requests.InvoiceAddress;
using Contracts.Requests.InvoiceData;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataGenerateRequestExample : IExamplesProvider<InvoiceDataGenerateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataGenerateRequest GetExamples()
    {
        return new InvoiceDataGenerateRequest()
        {
            UserId = Guid.Parse("2271c7b3-69ed-4e0c-a820-ecafbd57ac80"),
            SellerAddressId = Guid.Parse("c85f22c7-1323-423a-a402-9b711a44c119"),
            CustomerAddressId = Guid.Parse("b1f796e8-d23d-4b8b-b464-5e121e4385c7"),
            ItemsId = new ()
            {
                Guid.Parse("f8ff867d-8e7f-4c4d-b7f3-0f7ac1ecfd43"),
                Guid.Parse("8c24bb2a-25f1-4d33-801e-1d79ff9e2958"),
                Guid.Parse("87b3b388-9e67-4a0d-b7f3-bcb4b458ab29")                
            }
        };
    }
}
