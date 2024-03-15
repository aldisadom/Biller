using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Requests.InvoiceData;

public class InvoiceDataGenerateRequest
{
    [Required]
    public Guid SellerAddressId { get; set; }
    [Required]
    public Guid CustomerAddressId { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    [MinLength(1)]
    public List<Guid> ItemsId { get; set; } = new();
}
