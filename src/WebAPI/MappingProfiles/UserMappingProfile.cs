using Application.Models;
using AutoMapper;
using Contracts.Requests.User;
using Contracts.Responses.User;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class UserMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public UserMappingProfile()
    {
        //source, destination
        CreateMap<UserEntity, UserModel>();
        CreateMap<UserModel, UserEntity>();

        CreateMap<UserAddRequest, UserModel>();
        CreateMap<UserModel, UserAddResponse>();

        CreateMap<UserUpdateRequest, UserModel>();

        CreateMap<UserModel, UserResponse>();
    }
}
