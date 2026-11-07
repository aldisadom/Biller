using Domain.Entities;

namespace Domain.Repositories;

public interface ICustomerRepository
{
    Task<CustomerEntity?> Get(Guid id);
    Task<(IEnumerable<CustomerEntity> Items, int TotalCount, int Page)> Get(Guid? sellerId, int page, int pageSize);
    Task<Guid> Add(CustomerEntity customer);
    Task<bool?> Update(CustomerEntity customer);
    Task Delete(Guid id);
}
