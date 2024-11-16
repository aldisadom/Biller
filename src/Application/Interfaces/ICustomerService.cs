using Application.Models;
using Common;
using Contracts.Requests.Customer;

namespace Application.Interfaces;

public interface ICustomerService
{
    Task<Guid> Add(CustomerModel Customer);
    Task Delete(Guid id);
    Task<IEnumerable<CustomerModel>> Get(CustomerGetRequest? query);
    Task<CustomerModel> Get(Guid id);
    Task<Result<CustomerModel>> GetWithValidation(Guid id, Guid sellerId);
    Task Update(CustomerModel Customer);
    Task IncreaseInvoiceNumber(Guid id);
}
