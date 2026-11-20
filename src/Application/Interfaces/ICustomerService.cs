using Application.Models;
using BillerContracts.Requests.Customer;

namespace Application.Interfaces;

public interface ICustomerService
{
    Task<Guid> Add(CustomerModel customer);
    Task Delete(Guid id);
    Task<(IEnumerable<CustomerModel> Items, int TotalCount, int Page)> Get(CustomerGetRequest? query);
    Task<CustomerModel> Get(Guid id);
    Task<CustomerModel> GetForSeller(Guid id, Guid sellerId);
    Task Update(CustomerModel Customer);
}
