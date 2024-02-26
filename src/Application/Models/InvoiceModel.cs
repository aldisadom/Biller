namespace Application.Models;

internal class InvoiceModel
{
    public int InvoiceNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public required InvoiceClientModel SellerAddress { get; set; }
    public required InvoiceClientModel CustomerAddress { get; set; }
    public required List<InvoiceItemModel> Items { get; set; }
    public string Comments { get; set; } = string.Empty;
}
