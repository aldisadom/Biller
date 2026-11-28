using Application.Models;
using BillerContracts.Requests.Item;
using BillerContracts.Responses.Item;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class ItemModelExtensions
{
    // No Version: it is read only. ItemRepository.Add writes version 1, and Update the latest + 1
    // when the name or price changes.
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
            Quantity = from.Quantity,
            Version = from.Version
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
            Quantity = from.Quantity,
            Version = from.Version
        };
    }

    public static ItemVersionModel ToModel(this ItemVersionEntity from)
    {
        return new ItemVersionModel
        {
            Version = from.Version,
            Name = from.Name,
            Price = from.Price,
            ChangedDate = from.ChangedDate
        };
    }

    public static ItemVersionResponse ToResponse(this ItemVersionModel from)
    {
        return new ItemVersionResponse
        {
            Version = from.Version,
            Name = from.Name,
            Price = from.Price,
            ChangedDate = from.ChangedDate
        };
    }
}
