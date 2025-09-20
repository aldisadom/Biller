using Application.Models;
using Contracts.Requests.Seller;
using Contracts.Responses.Seller;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class SellerModelExtensions
{
    public static SellerEntity ToEntity(this SellerModel from)
    {
        return new SellerEntity
        {
            Id = from.Id,
            UserId = from.UserId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone,
            BankName = from.BankName,
            BankNumber = from.BankNumber
        };
    }

    public static SellerModel ToModel(this SellerEntity from)
    {
        return new SellerModel
        {
            Id = from.Id,
            UserId = from.UserId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone,
            BankName = from.BankName,
            BankNumber = from.BankNumber
        };
    }

    public static SellerModel ToModel(this SellerAddRequest from)
    {
        return new SellerModel
        {
            UserId = from.UserId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone,
            BankName = from.BankName,
            BankNumber = from.BankNumber
        };
    }

    public static SellerModel ToModel(this SellerUpdateRequest from)
    {
        return new SellerModel
        {
            Id = from.Id,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone,
            BankName = from.BankName,
            BankNumber = from.BankNumber
        };
    }

    public static SellerResponse ToResponse(this SellerModel from)
    {
        return new SellerResponse
        {
            Id = from.Id,
            UserId = from.UserId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone,
            BankName = from.BankName,
            BankNumber = from.BankNumber
        };
    }

    public static SellerModel ToModel(this SellerResponse from)
    {
        return new SellerModel
        {
            Id = from.Id,
            UserId = from.UserId,
            CompanyName = from.CompanyName,
            CompanyNumber = from.CompanyNumber,
            Street = from.Street,
            City = from.City,
            State = from.State,
            Email = from.Email,
            Phone = from.Phone,
            BankName = from.BankName,
            BankNumber = from.BankNumber
        };
    }
}
