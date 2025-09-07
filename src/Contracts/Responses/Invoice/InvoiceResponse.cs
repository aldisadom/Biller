using Common.Enums;
using Contracts.Responses.Customer;
using Contracts.Responses.Seller;

namespace Contracts.Responses.Invoice;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int InvoiceNumber { get; set; }
    public DateOnly CreatedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public SellerResponse? Seller { get; set; }
    public CustomerResponse? Customer { get; set; }
    public List<InvoiceItemResponse>? Items { get; set; }
    public string? Comments { get; set; }
    public decimal TotalPrice { get; set; }
    public InvoiceStatus Status { get; set; }
}
