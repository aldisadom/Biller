using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Contracts.Responses.InvoiceData;

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

        CreateMap<InvoiceItemModel, InvoiceItemResponse>(MemberList.Destination);

        //source, destination
        CreateMap<InvoiceDataModel, InvoiceDataResponse>(MemberList.Destination)
           .ForMember(x => x.TotalPrice, opt => opt.Ignore());
    }
}
