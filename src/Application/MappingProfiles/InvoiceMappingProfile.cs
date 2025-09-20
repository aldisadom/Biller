using Application.Models;
using Contracts.Requests.Invoice;
using Contracts.Responses.Invoice;
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
            InvoiceNumber = from.InvoiceNumber,
            User = JsonConvert.DeserializeObject<UserModel>(from.UserData),
            Seller = JsonConvert.DeserializeObject<SellerModel>(from.SellerData),
            Customer = JsonConvert.DeserializeObject<CustomerModel>(from.CustomerData),
            Items = JsonConvert.DeserializeObject<List<InvoiceItemModel>>(from.ItemsData),
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
            InvoiceNumber = from.InvoiceNumber,
            UserId = from.User!.Id,
            UserData = JsonConvert.SerializeObject(from.User),
            SellerId = from.Seller!.Id,
            SellerData = JsonConvert.SerializeObject(from.Seller),
            CustomerId = from.Customer!.Id,
            CustomerData = JsonConvert.SerializeObject(from.Customer),
            ItemsData = JsonConvert.SerializeObject(from.Items),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate.ToDateTime(TimeOnly.MinValue),
            DueDate = from.DueDate.ToDateTime(TimeOnly.MinValue),
            TotalPrice = from.CalculateTotal(),
            FilePath = from.GenerateFileLocation()
        };
    }

    public static InvoiceResponse ToResponse(this InvoiceModel from)
    {
        return new InvoiceResponse
        {
            Id = from.Id,
            InvoiceNumber = from.InvoiceNumber,
            UserId = from.User!.Id,
            Seller = from.Seller!.ToResponse(),
            Customer = from.Customer!.ToResponse(),
            Items = from.Items!.Select(i => i.ToInvoiceItemResponse()).ToList(),
            Comments = from.Comments,
            Status = from.Status,
            CreatedDate = from.CreatedDate,
            DueDate = from.DueDate,
            TotalPrice = from.CalculateTotal()
        };
    }

    public static InvoiceModel ToModel(this InvoiceAddRequest from)
    {
        return new InvoiceModel
        {
            User = new() { Id = from.UserId },
            Seller = new() { Id = from.SellerId },
            Customer = new() { Id = from.CustomerId },
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

    public static InvoiceItemModel ToInvoiceItemModel(this InvoiceItemUpdateRequest from)
    {
        return new InvoiceItemModel
        {
            Id = from.Id,
            Name = from.Name,
            Price = from.Price,
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
            Seller = from.Seller.ToModel(),
            Customer = from.Customer.ToModel(),
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
