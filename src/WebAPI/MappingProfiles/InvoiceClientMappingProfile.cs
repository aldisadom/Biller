using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceClient;
using Contracts.Responses.InvoiceClient;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class InvoiceClientMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public InvoiceClientMappingProfile()
    {
        //source, destination
        CreateMap<InvoiceClientModel, InvoiceClientEntity>(MemberList.Source);
        CreateMap<InvoiceClientEntity, InvoiceClientModel>(MemberList.Destination);

        CreateMap<InvoiceClientAddRequest, InvoiceClientModel>(MemberList.Source);

        CreateMap<InvoiceClientUpdateRequest, InvoiceClientModel>(MemberList.Source);

        CreateMap<InvoiceClientModel, InvoiceClientResponse>(MemberList.Destination);
    }
}
