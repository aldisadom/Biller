using Application.Models;
using AutoMapper;
using Contracts.Requests.Seller;
using Contracts.Responses.Seller;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class SellerMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public SellerMappingProfile()
    {
        //source, destination (which parameters must be mapped)
        CreateMap<SellerModel, SellerEntity>(MemberList.Source);
        CreateMap<SellerEntity, SellerModel>(MemberList.Destination);

        CreateMap<SellerAddRequest, SellerModel>(MemberList.Source);

        CreateMap<SellerUpdateRequest, SellerModel>(MemberList.Source);

        CreateMap<SellerModel, SellerResponse>(MemberList.Destination);
    }
}
