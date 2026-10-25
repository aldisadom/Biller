using Application.Models;
using BillerContracts.Requests.Customer;
using BillerContracts.Responses.Customer;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class CustomerModelExtensions
{
    public static CustomerEntity ToEntity(this CustomerModel from)
    {
        return new CustomerEntity
        {
            Id = from.Id,
            SellerId = from.SellerId,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone
        };
    }

    public static CustomerModel ToModel(this CustomerEntity from)
    {
        return new CustomerModel
        {
            Id = from.Id,
            SellerId = from.SellerId,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone
        };
    }

    public static CustomerModel ToModel(this CustomerAddRequest from)
    {
        return new CustomerModel
        {
            SellerId = from.SellerId,
            SeriesId = from.SeriesId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone
        };
    }

    public static CustomerModel ToModel(this CustomerUpdateRequest from)
    {
        return new CustomerModel
        {
            Id = from.Id,
            SeriesId = from.SeriesId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone
        };
    }

    public static CustomerResponse ToResponse(this CustomerModel from)
    {
        return new CustomerResponse
        {
            Id = from.Id,
            SellerId = from.SellerId,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone
        };
    }

    public static CustomerModel ToModel(this CustomerResponse from)
    {
        return new CustomerModel
        {
            Id = from.Id,
            SellerId = from.SellerId,
            SeriesId = from.SeriesId,
            SeriesName = from.SeriesName,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone
        };
    }
}
