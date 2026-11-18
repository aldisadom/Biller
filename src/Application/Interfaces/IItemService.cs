using Application.Models;
using BillerContracts;
using BillerContracts.Requests.Item;

namespace Application.Interfaces;

public interface IItemService
{
    Task<Guid> Add(ItemModel item);
    Task Delete(Guid id);
    Task<(IEnumerable<ItemModel> Items, int TotalCount, int Page)> Get(ItemGetRequest? query);
    Task<Result<IEnumerable<ItemVersionModel>>> GetVersionsWithValidation(List<InvoiceItemModel> lines, Guid customerId);
    Task<ItemModel> Get(Guid id);
    Task<ItemModel> Get(Guid id, int version);
    Task<IEnumerable<ItemVersionModel>> GetVersions(Guid id);
    Task Update(ItemModel item);
}
