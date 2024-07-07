using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Contracts.Responses.InvoiceData;
using Domain.Entities;
using Newtonsoft.Json;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class InvoiceDataMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public InvoiceDataMappingProfile()
    {
        //source, destination
        CreateMap<InvoiceItemRequest, InvoiceItemModel>(MemberList.Source);
        CreateMap<InvoiceItemUpdateRequest, InvoiceItemModel>(MemberList.Source);

        CreateMap<InvoiceItemModel, InvoiceItemResponse>(MemberList.Destination);

        //source, destination
        CreateMap<InvoiceDataModel, InvoiceDataResponse>(MemberList.Destination)
           .ForMember(dest => dest.TotalPrice, opts => opts.MapFrom(src => src.CalculateTotal()));

        CreateMap<InvoiceDataModel, InvoiceDataEntity>(MemberList.Destination)
           .ForMember(dest => dest.SellerId, opts => opts.MapFrom(src => src.Seller!.Id))
           .ForMember(dest => dest.CustomerId, opts => opts.MapFrom(src => src.Customer!.Id))
           .ForMember(dest => dest.UserId, opts => opts.MapFrom(src => src.User!.Id))
           .ForMember(dest => dest.SellerData, opts => opts.MapFrom(src => JsonConvert.SerializeObject(src.Seller)))
           .ForMember(dest => dest.CustomerData, opts => opts.MapFrom(src => JsonConvert.SerializeObject(src.Customer)))
           .ForMember(dest => dest.UserData, opts => opts.MapFrom(src => JsonConvert.SerializeObject(src.User)))
           .ForMember(dest => dest.ItemsData, opts => opts.MapFrom(src => JsonConvert.SerializeObject(src.Items)))
           .ForMember(dest => dest.TotalPrice, opts => opts.MapFrom(src => src.CalculateTotal()))
           .ForMember(dest => dest.FilePath, opts => opts.MapFrom(src => src.GenerateFileLocation()));

        CreateMap<InvoiceDataEntity, InvoiceDataModel>(MemberList.Destination)
           .ForMember(dest => dest.Seller, opts => opts.MapFrom(src => JsonConvert.DeserializeObject<SellerModel>(src.SellerData)))
           .ForMember(dest => dest.Customer, opts => opts.MapFrom(src => JsonConvert.DeserializeObject<CustomerModel>(src.CustomerData)))
           .ForMember(dest => dest.User, opts => opts.MapFrom(src => JsonConvert.DeserializeObject<UserModel>(src.UserData)))
           .ForMember(dest => dest.Items, opts => opts.MapFrom(src => JsonConvert.DeserializeObject<List<InvoiceItemModel>>(src.ItemsData)));


        CreateMap<InvoiceDataUpdateRequest, InvoiceDataModel>(MemberList.Source)
            .ForPath(dest => dest.User!.Id, opts => opts.MapFrom(src => src.UserId));

    }
}
