using Application.Models;
using Contracts.Requests.Customer;

namespace Application.Interfaces;

public interface ICustomerService
{
    Task<Guid> Add(CustomerModel Customer);
    Task Delete(Guid id);
    Task<IEnumerable<CustomerModel>> Get(CustomerGetRequest? query);
    Task<CustomerModel> Get(Guid id);
    Task Update(CustomerModel Customer);
    Task IncreaseInvoiceNumber(Guid id);
}
