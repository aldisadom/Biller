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
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            InvoiceNumber = from.InvoiceNumber,
            UserId = from.UserId,
            SellerId = from.SellerId,
            SellerName = from.Seller.CompanyName,
            CustomerId = from.CustomerId,
            CustomerName = from.Customer.CompanyName,
            Items = from.Items!.Select(i => i.ToInvoiceItemResponse()).ToList(),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate,
            DueDate = from.DueDate,
            TotalPrice = from.CalculateTotal()
        };
    }
}
