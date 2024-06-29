using Contracts.Responses.Customer;
using Contracts.Responses.InvoiceData;
using Contracts.Responses.Seller;
using Swashbuckle.AspNetCore.Filters;
using System.Xml.Linq;

namespace WebAPI.SwaggerExamples.InvoiceData;

/// <summary>
/// example
/// </summary>
public class InvoiceDataListResponseExample : IExamplesProvider<InvoiceDataListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public InvoiceDataListResponse GetExamples()
    {
        InvoiceDataListResponse invoiceDataListResponse = new();

        /*
         * public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime DueDate { get; set; }
    public SellerResponse? Seller { get; set; }
    public CustomerResponse? Customer { get; set; }
    public List<InvoiceItemResponse>? Items { get; set; }
    public string? Comments { get; set; }
    public decimal TotalPrice { get; set; }
        */

        invoiceDataListResponse.Invoices.Add(new InvoiceDataResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Number = "0001",
            CreatedDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(10),
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

        invoiceDataListResponse.Invoices.Add(new InvoiceDataResponse()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Number = "0005",
            CreatedDate = DateTime.Now.AddDays(-1),
            DueDate = DateTime.Now.AddDays(10),
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
