using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceItem;
using Contracts.Responses.InvoiceItem;
using Contracts.Responses.User;
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
        CreateMap<InvoiceItemEntity, InvoiceItemModel>();
        CreateMap<InvoiceItemModel, InvoiceItemEntity>();

        CreateMap<InvoiceItemAddRequest, InvoiceItemModel>();
        CreateMap<InvoiceItemModel, InvoiceItemAddResponse>();

        CreateMap<InvoiceItemUpdateRequest, InvoiceItemModel>();

        CreateMap<InvoiceItemModel, InvoiceItemResponse>();
    }
}
