using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceAddress;
using Contracts.Requests.InvoiceData;
using Contracts.Responses.InvoiceAddress;
using Domain.Entities;

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
