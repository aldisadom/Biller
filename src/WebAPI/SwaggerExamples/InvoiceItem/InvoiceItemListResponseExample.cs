using Contracts.Responses.InvoiceItem;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.InvoiceItem;

/// <summary>
/// example
/// </summary>
public class InvoiceItemListResponseExample : IExamplesProvider<InvoiceItemListResponse>
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
            Id = Guid.NewGuid(),
            Name = "Iron throne",
            Price = 999999999999999m
        });

        ItemListResponse.InvoiceItems.Add(new InvoiceItemResponse()
        {
            Id = Guid.NewGuid(),
            Name = "Lanister backstab",
            Price = 0.01m
        });

        ItemListResponse.InvoiceItems.Add(new InvoiceItemResponse()
        {
            Id = Guid.NewGuid(),
            Name = "Hourly rate",
            Price = 10.00m
        });

        return ItemListResponse;
    }
}
