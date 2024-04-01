using Domain.Entities;

namespace Domain.Repositories;

public interface ICustomerRepository
{
    Task<CustomerEntity?> Get(Guid id);
    Task<IEnumerable<CustomerEntity>> GetBySeller(Guid userId);
    Task<IEnumerable<CustomerEntity>> Get();
    Task<Guid> Add(CustomerEntity customer);
    Task Update(CustomerEntity customer);
    Task UpdateInvoiceNumber(CustomerEntity customer);
    Task Delete(Guid id);
}
