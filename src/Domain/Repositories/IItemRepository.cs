using Domain.Entities;

namespace Domain.Repositories;

public interface IItemRepository
{
    Task<ItemEntity?> Get(Guid id);
    Task<IEnumerable<ItemEntity>> Get(List<Guid> ids);
    Task<IEnumerable<ItemEntity>> GetByCustomerId(Guid customerId);
    Task<IEnumerable<ItemEntity>> Get();
    Task<Guid> Add(ItemEntity item);
    Task Update(ItemEntity item);
    Task Delete(Guid id);
}
