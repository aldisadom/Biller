using Application.Models;
using BillerContracts.Requests.Invoice;
using BillerContracts.Responses.Invoice;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class InvoiceModelExtensions
{
    public static InvoiceModel ToModel(this InvoiceEntity from)
    {
        return new InvoiceModel
        {
            Id = from.Id,
            UserId = from.UserId,
            CustomerId = from.CustomerId,
            SellerId = from.SellerId,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            InvoiceNumber = from.InvoiceNumber,
            Items = from.Items.OrderBy(ii => ii.LineNo).Select(ii => ii.ToInvoiceItemModel()).ToList(),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = DateOnly.FromDateTime(from.CreatedDate),
            DueDate = DateOnly.FromDateTime(from.DueDate)
        };
    }

    public static InvoiceEntity ToEntity(this InvoiceModel from)
    {
        return new InvoiceEntity
        {
            Id = from.Id,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            InvoiceNumber = from.InvoiceNumber,
            UserId = from.UserId,
            SellerId = from.SellerId,
            CustomerId = from.CustomerId,
            Items = from.Items.Select((item, index) => item.ToInvoiceItemEntity(from.Id, index + 1)).ToList(),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate.ToDateTime(TimeOnly.MinValue),
            DueDate = from.DueDate.ToDateTime(TimeOnly.MinValue)
        };
    }

    // Name and price come from the item version when read; an invoice item doesn't store them.
    public static InvoiceItemModel ToInvoiceItemModel(this InvoiceItemEntity from)
    {
        return new InvoiceItemModel
        {
            ItemId = from.ItemId,
            Version = from.Version,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity,
            Comments = from.Comments
        };
    }

    // An empty version stays empty: InvoiceRepository.Add takes the item's latest, and PUT requires one.
    public static InvoiceItemEntity ToInvoiceItemEntity(this InvoiceItemModel from, Guid invoiceId, int lineNo)
    {
        return new InvoiceItemEntity
        {
            InvoiceId = invoiceId,
            LineNo = lineNo,
            ItemId = from.ItemId,
            Version = from.Version,
            Quantity = from.Quantity,
            Comments = from.Comments
        };
    }

    public static InvoiceModelFull ToFull(this InvoiceModel from, UserModel user, SellerModel seller, CustomerModel customer)
    {
        return new InvoiceModelFull
        {
            Id = from.Id,
            UserId = from.UserId,
            SellerId = from.SellerId,
            CustomerId = from.CustomerId,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            InvoiceNumber = from.InvoiceNumber,
            Items = from.Items,
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate,
            DueDate = from.DueDate,
            User = user,
            Seller = seller,
            Customer = customer
        };
    }

    public static InvoiceModel ToModel(this InvoiceAddRequest from)
    {
        return new InvoiceModel
        {
            UserId = from.UserId,
            SellerId = from.SellerId,
            CustomerId = from.CustomerId,
            Items = from.Items.Select(i => i.ToInvoiceItemModel()).ToList(),
            Comments = from.Comments,
            CreatedDate = from.CreatedDate,
            DueDate = from.DueDate,
        };
    }

    public static InvoiceItemModel ToInvoiceItemModel(this InvoiceItemRequest from)
    {
        return new InvoiceItemModel
        {
            ItemId = from.Id,
            Quantity = from.Quantity,
            Comments = from.Comments,
            Version = from.Version
        };
    }

    public static InvoiceModel ToModel(this InvoiceUpdateRequest from)
    {
        return new InvoiceModel
        {
            Id = from.Id,
            SellerId = from.SellerId,
            CustomerId = from.CustomerId,
            Comments = from.Comments,
            CreatedDate = from.CreatedDate,
            DueDate = from.DueDate,
            Items = from.Items.Select(i => i.ToInvoiceItemModel()).ToList(),
        };
    }

    public static InvoiceItemResponse ToInvoiceItemResponse(this InvoiceItemModel from)
    {
        return new InvoiceItemResponse
        {
            Id = from.ItemId,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity,
            Comments = from.Comments,
            Version = from.Version
        };
    }
}
