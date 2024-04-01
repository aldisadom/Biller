using Application.Models;
using AutoMapper;
using Contracts.Requests.Customer;
using Contracts.Responses.Customer;
using Domain.Entities;

namespace WebAPI.MappingProfiles;

/// <summary>
/// Mapper profiles
/// </summary>
public class CustomerMappingProfile : Profile
{
    /// <summary>
    /// Mapper profile
    /// </summary>
    public CustomerMappingProfile()
    {
        //source, destination
        CreateMap<CustomerModel, CustomerEntity>(MemberList.Source);
        CreateMap<CustomerEntity, CustomerModel>(MemberList.Destination);

        CreateMap<CustomerAddRequest, CustomerModel>(MemberList.Source);

        CreateMap<CustomerUpdateRequest, CustomerModel>(MemberList.Source);

        CreateMap<CustomerModel, CustomerResponse>(MemberList.Destination);
    }
}
