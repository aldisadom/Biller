using Application.Models;
using BillerContracts.Requests.Invoice;
using BillerContracts.Responses.Invoice;
using Domain.Entities;
using Newtonsoft.Json;

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
            InvoiceNumber = from.InvoiceNumber,
            Items = JsonConvert.DeserializeObject<List<InvoiceItemModel>>(from.ItemsData),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = DateOnly.FromDateTime(from.CreatedDate),
            DueDate = DateOnly.FromDateTime(from.DueDate)
        };
    }

    public static InvoiceEntity ToEntity(this InvoiceModel from, string filePath)
    {
        return new InvoiceEntity
        {
            Id = from.Id,
            InvoiceNumber = from.InvoiceNumber,
            UserId = from.UserId,
            SellerId = from.SellerId,
            CustomerId = from.CustomerId,
            FilePath = filePath,
            ItemsData = JsonConvert.SerializeObject(from.Items),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate.ToDateTime(TimeOnly.MinValue),
            DueDate = from.DueDate.ToDateTime(TimeOnly.MinValue),
            TotalPrice = from.CalculateTotal()
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
            Id = from.Id,
            Quantity = from.Quantity,
            Comments = from.Comments
        };
    }

    public static InvoiceModel ToModel(this InvoiceUpdateRequest from)
    {
        return new InvoiceModel
        {
            Id = from.Id,
            InvoiceNumber = from.InvoiceNumber,
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
            Id = from.Id,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity,
            Comments = from.Comments
        };
    }
}
