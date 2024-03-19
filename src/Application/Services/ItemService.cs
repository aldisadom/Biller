using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.Item;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;

namespace Application.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly IMapper _mapper;

    public ItemService(IItemRepository itemRepository, IMapper mapper)
    {
        _itemRepository = itemRepository;
        _mapper = mapper;
    }

    public async Task<ItemModel> Get(Guid id)
    {
        ItemEntity itemEntity = await _itemRepository.Get(id)
            ?? throw new NotFoundException($"Invoice item:{id} not found");

        return _mapper.Map<ItemModel>(itemEntity);
    }

    public async Task<IEnumerable<ItemModel>> Get(List<Guid> ids)
    {
        IEnumerable<ItemEntity> itemEntities;

        itemEntities = await _itemRepository.Get(ids);

        return _mapper.Map<IEnumerable<ItemModel>>(itemEntities);
    }

    public async Task<IEnumerable<ItemModel>> Get(ItemGetRequest query)
    {
        IEnumerable<ItemEntity> itemEntities;

        if (query is null)
            itemEntities = await _itemRepository.Get();
        else if (query.AddressId is not null)
            itemEntities = await _itemRepository.GetByAddressId((Guid)query.AddressId);
        else
            itemEntities = await _itemRepository.Get();

        return _mapper.Map<IEnumerable<ItemModel>>(itemEntities);
    }

    public async Task<Guid> Add(ItemModel item)
    {
        ItemEntity itemEntity = _mapper.Map<ItemEntity>(item);

        return await _itemRepository.Add(itemEntity);
    }

    public async Task Update(ItemModel item)
    {
        await Get(item.Id);

        ItemEntity itemEntity = _mapper.Map<ItemEntity>(item);

        await _itemRepository.Update(itemEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _itemRepository.Delete(id);
    }
}
