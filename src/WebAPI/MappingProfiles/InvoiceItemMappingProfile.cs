using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceItem;
using Contracts.Responses.InvoiceItem;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class InvoiceItemMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public InvoiceItemMappingProfile()
    {
        //source, destination
        CreateMap<InvoiceItemModel, InvoiceItemEntity>(MemberList.Source);
        CreateMap<InvoiceItemEntity, InvoiceItemModel>(MemberList.Destination);

        CreateMap<InvoiceItemAddRequest, InvoiceItemModel>(MemberList.Source);

        CreateMap<InvoiceItemUpdateRequest, InvoiceItemModel>(MemberList.Source);

        CreateMap<InvoiceItemModel, InvoiceItemResponse>(MemberList.Destination);
    }
}
