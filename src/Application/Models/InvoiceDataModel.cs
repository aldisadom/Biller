namespace Application.Models;

public class InvoiceDataModel
{
    public Guid SellerAddressId { get; set; }
    public Guid CustomerAddressId { get; set; }
    public Guid UserId { get; set; }
    public List<Guid> ItemsId { get; set; } = new();
}
