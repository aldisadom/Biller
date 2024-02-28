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
        CreateMap<UserModel, UserEntity>(MemberList.Source);
        CreateMap<UserEntity, UserModel>(MemberList.Destination);

        CreateMap<UserAddRequest, UserModel>(MemberList.Source);

        CreateMap<UserUpdateRequest, UserModel>(MemberList.Source);

        CreateMap<UserModel, UserResponse>(MemberList.Destination);
    }
}
