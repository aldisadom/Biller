using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;

    public ItemService(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<ItemModel> Get(Guid id)
    {
        ItemEntity itemEntity = await _itemRepository.Get(id)
            ?? throw new NotFoundException("Item not found in DB");

        ItemModel itemResponse = new()
        {
            Id = id,
            Name = itemEntity.Name,
            Price = itemEntity.Price,
            ShopId = itemEntity.ShopId,
        };

        return itemResponse;
    }

    public async Task<IEnumerable<ItemModel>> Get()
    {
        IEnumerable<ItemEntity> itemEntities = await _itemRepository.Get();

        IEnumerable<ItemModel> items = itemEntities.Select(i => new ItemModel()
        {
            Id = i.Id,
            Name = i.Name,
            Price = i.Price,
            ShopId = i.ShopId,
        });

        return items;
    }

    public async Task<Guid> Add(ItemModel item)
    {
        ItemEntity itemEntity = new()
        {
            Name = item.Name,
            Price = item.Price,
        };

        return await _itemRepository.Add(itemEntity);
    }

    public async Task Update(ItemModel item)
    {
        await Get(item.Id);

        ItemEntity itemEntity = new ItemEntity()
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            ShopId = item.ShopId,
        };

        await _itemRepository.Update(itemEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _itemRepository.Delete(id);
    }
}