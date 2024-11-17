namespace Application.Models;

public record InvoiceModel
{
    public Guid Id { get; set; }
    public UserModel? User { get; set; }
    public DateOnly CreatedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public SellerModel? Seller { get; set; }
    public CustomerModel? Customer { get; set; }
    public List<InvoiceItemModel>? Items { get; set; }
    public int InvoiceNumber { get; set; }
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
        return $"./Data/Invoices";//{User!.Id}/{Seller!.Id}/{Customer!.Id}";
    }

    public string GenerateFileLocation()
    {
        return $"{GenerateFolderLocation()}/{Customer!.InvoiceName}-{GenerateInvoiceName()}.pdf";
    }

    public string GenerateFileLocation(string modifier)
    {
        return $"{GenerateFolderLocation()}/{Customer!.InvoiceName}-{GenerateInvoiceName()}-{modifier}.pdf";
    }

    public string GenerateInvoiceName()
    {
        string name = string.Empty;
        if (Customer!.InvoiceNumber < 10)
            name = "00000";
        else if (Customer.InvoiceNumber < 100)
            name = "0000";
        else if (Customer.InvoiceNumber < 1000)
            name = "000";
        else if (Customer.InvoiceNumber < 10000)
            name = "00";
        else if (Customer.InvoiceNumber < 100000)
            name = "0";

        name += Customer.InvoiceNumber.ToString();
        return name;
    }
}
