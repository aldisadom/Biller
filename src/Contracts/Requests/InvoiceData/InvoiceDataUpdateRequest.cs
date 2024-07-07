using Contracts.Requests.Customer;
using Contracts.Requests.Seller;
using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.InvoiceData;

public class InvoiceDataUpdateRequest
{
    public Guid Id { get; set; }
    [Required]
    public required SellerUpdateRequest Seller { get; set; }
    [Required]
    public required CustomerUpdateRequest Customer { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    [MinLength(1)]
    public List<InvoiceItemUpdateRequest> Items { get; set; } = [];
    public string Comments { get; set; } = string.Empty;
    [Required]
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
