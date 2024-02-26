using Contracts.Responses.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemListResponseExample : IExamplesProvider<InvoiceItemListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceItemListResponse GetExamples()
    {
        InvoiceItemListResponse ItemListResponse = new();

        ItemListResponse.InvoiceItems.Add(new InvoiceItemResponse()
        {
            Id = Guid.Parse("51427c65-fb49-42be-a651-a0a1dee84931"),
            Name = "Toy car",
            Price = 5.11m
        });

        ItemListResponse.InvoiceItems.Add(new InvoiceItemResponse()
        {
            Id = Guid.Parse("d4ec61ea-b5d3-4967-aa76-23f3990de955"),
            Name = "Hourly rate",
            Price = 10.00m
        });

        return ItemListResponse;
    }
}
