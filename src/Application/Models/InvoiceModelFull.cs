namespace Application.Models;

public record InvoiceModelFull : InvoiceModel
{
    public required UserModel User { get; set; }
    public required SellerModel Seller { get; set; }
    public required CustomerModel Customer { get; set; }

    public string GenerateFileLocation()
    {
        return GenerateFileLocation(Customer.InvoiceName);
    }
}
