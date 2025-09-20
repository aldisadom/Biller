using Application.Models;
using Contracts.Requests.Item;
using Contracts.Responses.Item;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class ItemModelExtensions
{
    public static ItemEntity ToEntity(this ItemModel from)
    {
        return new ItemEntity
        {
            Id = from.Id,
            CustomerId = from.CustomerId,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity
        };
    }

    public static ItemModel ToModel(this ItemEntity from)
    {
        return new ItemModel
        {
            Id = from.Id,
            CustomerId = from.CustomerId,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity
        };
    }

    public static ItemModel ToModel(this ItemAddRequest from)
    {
        return new ItemModel
        {
            CustomerId = from.CustomerId,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity
        };
    }

    public static ItemModel ToModel(this ItemUpdateRequest from)
    {
        return new ItemModel
        {
            Id = from.Id,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity
        };
    }

    public static ItemResponse ToResponse(this ItemModel from)
    {
        return new ItemResponse
        {
            Id = from.Id,
            CustomerId = from.CustomerId,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity
        };
    }

    public static ItemModel ToModel(this ItemResponse from)
    {
        return new ItemModel
        {
            Id = from.Id,
            CustomerId = from.CustomerId,
            Name = from.Name,
            Price = from.Price,
            Quantity = from.Quantity
        };
    }
}
