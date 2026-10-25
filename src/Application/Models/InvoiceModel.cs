using BillerContracts.Enums;

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
    public Guid SeriesId { get; set; }
    public string SeriesName { get; set; } = string.Empty;
    public int InvoiceNumber { get; set; }
    public string? Comments { get; set; }
    public InvoiceStatus Status { get; set; }

    public decimal CalculateTotal()
    {
        decimal total = 0;
        foreach (InvoiceItemModel item in Items!)
            total += item.Price * item.Quantity;

        return total;
    }

    public string GenerateFileLocation()
    {
        return $"{SeriesName}-{GenerateInvoiceName()}.pdf";
    }

    public string GenerateInvoiceName()
    {
        return InvoiceNumber.ToString("D6");
    }
}
