using Application.Models;
using BillerContracts.Requests.Series;
using BillerContracts.Responses.Series;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class SeriesModelExtensions
{
    public static SeriesEntity ToEntity(this SeriesModel from)
    {
        return new SeriesEntity
        {
            Id = from.Id,
            SellerId = from.SellerId,
            Name = from.Name,
            NextNumber = from.NextNumber
        };
    }

    public static SeriesModel ToModel(this SeriesEntity from)
    {
        return new SeriesModel
        {
            Id = from.Id,
            SellerId = from.SellerId,
            Name = from.Name,
            NextNumber = from.NextNumber
        };
    }

    public static SeriesModel ToModel(this SeriesAddRequest from)
    {
        return new SeriesModel
        {
            SellerId = from.SellerId,
            Name = from.Name,
            NextNumber = from.NextNumber ?? 1
        };
    }

    public static SeriesModel ToModel(this SeriesUpdateRequest from)
    {
        return new SeriesModel
        {
            Id = from.Id,
            Name = from.Name,
            NextNumber = from.NextNumber
        };
    }

    public static SeriesResponse ToResponse(this SeriesModel from)
    {
        return new SeriesResponse
        {
            Id = from.Id,
            SellerId = from.SellerId,
            Name = from.Name,
            NextNumber = from.NextNumber
        };
    }

    public static SeriesModel ToModel(this SeriesResponse from)
    {
        return new SeriesModel
        {
            Id = from.Id,
            SellerId = from.SellerId,
            Name = from.Name,
            NextNumber = from.NextNumber
        };
    }
}
