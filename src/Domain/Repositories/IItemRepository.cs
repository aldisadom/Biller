using Domain.Entities;

namespace Domain.Repositories;

public interface IItemRepository
{
    Task<ItemEntity?> Get(Guid id, int? version);
    Task<IEnumerable<ItemVersionEntity>> GetVersions(Guid itemId);
    Task<IEnumerable<Guid>> GetIdsForCustomer(List<Guid> ids, Guid customerId);
    Task<(IEnumerable<ItemEntity> Items, int TotalCount, int Page)> Get(Guid? customerId, int page, int pageSize);
    Task<Guid> Add(ItemEntity item);
    Task Update(ItemEntity item);
    Task Delete(Guid id);
}
