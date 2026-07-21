using Application.Models;
using BillerContracts;
using BillerContracts.Requests.Customer;

namespace Application.Interfaces;

public interface ICustomerService
{
    Task<Guid> Add(CustomerModel customer);
    Task Delete(Guid id);
    Task<IEnumerable<CustomerModel>> Get(CustomerGetRequest? query);
    Task<CustomerModel> Get(Guid id);
    Task<Result<CustomerModel>> GetWithValidation(Guid id, Guid sellerId);
    Task Update(CustomerModel Customer);
    Task IncreaseInvoiceNumber(Guid id);
}
