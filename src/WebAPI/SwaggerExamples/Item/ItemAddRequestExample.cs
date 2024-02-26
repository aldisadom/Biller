using Contracts.Requests.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemAddRequestExample : IExamplesProvider<InvoiceItemAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemAddRequest GetExamples()
    {
        return new InvoiceItemAddRequest()
        {
            Name = "Toy car",
            Price = 5.11m,
            ShopId = Guid.Parse("6a1e7354-e67f-4795-a174-4aacd51bacc3")
        };
    }
}
