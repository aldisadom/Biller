namespace Contracts.Requests.Invoice;

public class InvoiceAddRequest
{
    public Guid SellerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid UserId { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = [];
    public string Comments { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
