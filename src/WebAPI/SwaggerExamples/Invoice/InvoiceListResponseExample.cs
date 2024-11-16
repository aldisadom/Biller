using Contracts.Responses.Customer;
using Contracts.Responses.Invoice;
using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Invoice;

/// <summary>
/// example
/// </summary>
public class InvoiceListResponseExample : IExamplesProvider<InvoiceListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceListResponse GetExamples()
    {
        InvoiceListResponse invoiceDataListResponse = new();

        /*
         * public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly CreatedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public SellerResponse? Seller { get; set; }
    public CustomerResponse? Customer { get; set; }
    public List<InvoiceItemResponse>? Items { get; set; }
    public string? Comments { get; set; }
    public decimal TotalPrice { get; set; }
        */

        invoiceDataListResponse.Invoices.Add(new InvoiceResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            InvoiceNumber = 1,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
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
            Comments = "",
            TotalPrice = 1500m,
        });

        invoiceDataListResponse.Invoices.Add(new InvoiceResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            InvoiceNumber = 5,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
            DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
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
            Comments = "Pay in cash",
            TotalPrice = 250m,
        });

        return invoiceDataListResponse;
    }
}
