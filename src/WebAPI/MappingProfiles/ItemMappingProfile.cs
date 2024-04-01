using Application.Models;
using AutoMapper;
using Contracts.Requests.Item;
using Contracts.Responses.Item;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class ItemMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public ItemMappingProfile()
    {
        //source, destination
        CreateMap<ItemModel, ItemEntity>(MemberList.Source);
        CreateMap<ItemEntity, ItemModel>(MemberList.Destination);

        CreateMap<ItemAddRequest, ItemModel>(MemberList.Source);

        CreateMap<ItemUpdateRequest, ItemModel>(MemberList.Source);

        CreateMap<ItemModel, ItemResponse>(MemberList.Destination);
    }
}
