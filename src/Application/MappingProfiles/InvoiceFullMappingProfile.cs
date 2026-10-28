using Application.Models;
using BillerContracts.Responses.Invoice;

namespace Application.MappingProfiles;

public static class InvoiceModelFullExtensions
{
    public static InvoiceResponse ToResponse(this InvoiceModelFull from)
    {
        return new InvoiceResponse
        {
            Id = from.Id,
            InvoiceNumber = from.InvoiceNumber,
            UserId = from.UserId,
            Seller = from.Seller.ToResponse(),
            Customer = from.Customer.ToResponse(),
            Items = from.Items!.Select(i => i.ToInvoiceItemResponse()).ToList(),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate,
            DueDate = from.DueDate,
            TotalPrice = from.CalculateTotal()
        };
    }
}
