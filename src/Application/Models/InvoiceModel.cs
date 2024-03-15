namespace Application.Models;

public class InvoiceModel
{
    public int FilePath { get; set; }
    public int InvoiceName { get; set; }
    public int InvoiceNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceAddressModel SellerAddress { get; set; }
    public InvoiceAddressModel CustomerAddress { get; set; }
    public List<InvoiceItemModel> Items { get; set; }
    public string Comments { get; set; } = string.Empty;
}
