using Application.Models;

namespace Application.Interfaces;

public interface IItemService
{
    Task<Guid> Add(ItemModel item);
    Task Delete(Guid id);
    Task<IEnumerable<ItemModel>> Get();
    Task<ItemModel> Get(Guid id);
    Task Update(ItemModel item);
}