using Application.Models;
using Common;
using Contracts.Requests.Item;

namespace Application.Interfaces;

public interface IItemService
{
    Task<Guid> Add(ItemModel item);
    Task Delete(Guid id);
    Task<IEnumerable<ItemModel>> Get(ItemGetRequest? query);
    Task<IEnumerable<ItemModel>> Get(List<Guid> ids);
    Task<Result<IEnumerable<ItemModel>>> GetWithValidation(List<Guid> ids, Guid customerId);
    Task<ItemModel> Get(Guid id);
    Task Update(ItemModel item);
}
