using Application.Models;
using Contracts.Requests.User;
using Contracts.Responses.User;
using Domain.Entities;

namespace Application.MappingProfiles;

public static class UserModelExtensions
{
    public static UserEntity ToEntity(this UserModel from)
    {
        return new UserEntity
        {
            Id = from.Id,
            Name = from.Name,
            LastName = from.LastName,
            Email = from.Email,
            Password = from.Password
        };
    }

    public static UserModel ToModel(this UserEntity from)
    {
        return new UserModel
        {
            Id = from.Id,
            Name = from.Name,
            LastName = from.LastName,
            Email = from.Email,
            Password = from.Password
        };
    }

    public static UserModel ToModel(this UserAddRequest from)
    {
        return new UserModel
        {
            Name = from.Name,
            LastName = from.LastName,
            Email = from.Email,
            Password = from.Password
        };
    }

    public static UserModel ToModel(this UserUpdateRequest from)
    {
        return new UserModel
        {
            Id = from.Id,
            Name = from.Name,
            LastName = from.LastName
        };
    }

    public static UserResponse ToResponse(this UserModel from)
    {
        return new UserResponse
        {
            Id = from.Id,
            Name = from.Name,
            LastName = from.LastName,
            Email = from.Email
        };
    }

    public static UserModel ToModel(this UserLoginRequest from)
    {
        return new UserModel
        {
            Email = from.Email,
            Password = from.Password
        };
    }
}
