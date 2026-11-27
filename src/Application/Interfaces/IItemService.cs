using Application.Models;
using BillerContracts.Requests.Item;

namespace Application.Interfaces;

public interface IItemService
{
    Task<Guid> Add(ItemModel item);
    Task Delete(Guid id);
    Task<(IEnumerable<ItemModel> Items, int TotalCount, int Page)> Get(ItemGetRequest? query);
    Task CheckForCustomer(List<Guid> ids, Guid customerId);
    Task<ItemModel> Get(Guid id, int? version);
    Task<IEnumerable<ItemVersionModel>> GetVersions(Guid id);
    Task Update(ItemModel item);
}
