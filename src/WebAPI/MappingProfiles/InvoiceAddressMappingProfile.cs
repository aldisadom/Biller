using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceAddress;
using Contracts.Responses.InvoiceAddress;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class InvoiceAddressMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public InvoiceAddressMappingProfile()
    {
        //source, destination
        CreateMap<InvoiceAddressModel, InvoiceAddressEntity>(MemberList.Source);
        CreateMap<InvoiceAddressEntity, InvoiceAddressModel>(MemberList.Destination);

        CreateMap<InvoiceAddressAddRequest, InvoiceAddressModel>(MemberList.Source);

        CreateMap<InvoiceAddressUpdateRequest, InvoiceAddressModel>(MemberList.Source);

        CreateMap<InvoiceAddressModel, InvoiceAddressResponse>(MemberList.Destination);
    }
}
