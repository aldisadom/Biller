namespace Application.Models;

public class InvoiceDataModel
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public UserModel? User { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime DueDate { get; set; }
    public SellerModel? Seller { get; set; }
    public CustomerModel? Customer { get; set; }
    public List<InvoiceItemModel>? Items { get; set; }
    public string? Comments { get; set; }

    public decimal CalculateTotal()
    {
        decimal total = 0;
        foreach (InvoiceItemModel item in Items!)
            total += item.Price * item.Quantity;

        return total;
    }

    public string GenerateFolderLocation()
    {
        return $"Data/Invoices/{User!.Id}/{Seller!.Id}/{Customer!.Id}";
    }

    public string GenerateFileLocation()
    {
        return $"{GenerateFolderLocation()}/{Customer!.InvoiceName}-{Number}.pdf";
    }

    public string GenerateFileLocation(string modifier)
    {
        return $"{GenerateFolderLocation()}/{Customer!.InvoiceName}-{Number}-{modifier}.pdf";
    }
}
