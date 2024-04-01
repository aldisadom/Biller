namespace Application.Models;

public class InvoiceModel
{
    public string FilePath { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public SellerModel Seller { get; set; }
    public CustomerModel Customer { get; set; }
    public List<ItemModel> Items { get; set; }
    public string Comments { get; set; } = string.Empty;
}
