using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Item;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using System.Net;

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
            ?? throw new NotFoundException($"Invoice item:{id} not found");

        return itemEntity.ToModel();
    }

    public async Task<IEnumerable<ItemModel>> Get(List<Guid> ids)
    {
        IEnumerable<ItemEntity> itemEntities;

        itemEntities = await _itemRepository.Get(ids);

        return itemEntities.Select(i => i.ToModel());
    }

    public async Task<(IEnumerable<ItemModel> Items, int TotalCount, int Page)> Get(ItemGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount, returnedPage) = await _itemRepository.Get(query?.CustomerId, page, (int)pageSize);

        return (entities.Select(i => i.ToModel()), totalCount, returnedPage);
    }

    public async Task<Result<IEnumerable<ItemModel>>> GetWithValidation(List<Guid> ids, Guid customerId)
    {
        var items = await _itemRepository.Get(ids);

        string idString = string.Empty;

        var nonMatching = ids
            .Where(id => !items.Any(i => (i.Id == id) && (i.CustomerId == customerId)))
            .ToList();

        foreach (var item in nonMatching)
            idString += " " + item.ToString();

        if (!string.IsNullOrEmpty(idString))
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Items id{idString} is invalid for customer id {customerId}" };

        return items.ToList().Select(i => i.ToModel()).ToList();
    }

    public async Task<Guid> Add(ItemModel item)
    {
        ItemEntity itemEntity = item.ToEntity();

        return await _itemRepository.Add(itemEntity);
    }

    public async Task Update(ItemModel item)
    {
        await Get(item.Id);

        ItemEntity itemEntity = item.ToEntity();

        await _itemRepository.Update(itemEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _itemRepository.Delete(id);
    }
}
