using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceData;

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
        CreateMap<InvoiceDataGenerateRequest, InvoiceDataModel>(MemberList.Source);
    }
}
