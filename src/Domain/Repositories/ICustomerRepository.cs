using Domain.Entities;

namespace Domain.Repositories;

public interface ICustomerRepository
{
    Task<CustomerEntity?> Get(Guid id);
    Task<IEnumerable<CustomerEntity>> GetBySellerId(Guid sellerId);
    Task<IEnumerable<CustomerEntity>> Get();
    Task<Guid> Add(CustomerEntity customer);
    Task Update(CustomerEntity customer);
    Task IncreaseInvoiceNumber(Guid id);
    Task Delete(Guid id);
}
