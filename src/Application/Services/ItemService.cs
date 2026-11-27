using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Item;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;

namespace Application.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;

    public ItemService(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<ItemModel> Get(Guid id, int? version)
    {
        ItemEntity itemEntity = await _itemRepository.Get(id, version)
            ?? throw new NotFoundException(version is null
                ? $"Invoice item:{id} not found"
                : $"Invoice item:{id} version {version} not found");

        return itemEntity.ToModel();
    }

    public async Task<IEnumerable<ItemVersionModel>> GetVersions(Guid id)
    {
        List<ItemVersionEntity> versions = (await _itemRepository.GetVersions(id)).ToList();

        if (versions.Count == 0)
            throw new NotFoundException($"Invoice item:{id} not found");

        return versions.Select(v => v.ToModel());
    }


    public async Task<(IEnumerable<ItemModel> Items, int TotalCount, int Page)> Get(ItemGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount, returnedPage) = await _itemRepository.Get(query?.CustomerId, page, (int)pageSize);

        return (entities.Select(i => i.ToModel()), totalCount, returnedPage);
    }

    public async Task CheckForCustomer(List<Guid> ids, Guid customerId)
    {
        HashSet<Guid> ownItemIds = (await _itemRepository.GetIdsForCustomer(ids, customerId)).ToHashSet();
        List<Guid> invalidIds = ids.Where(id => !ownItemIds.Contains(id)).ToList();

        if (invalidIds.Count != 0)
            throw new ValidationException($"Items id {string.Join(" ", invalidIds)} is invalid for customer id {customerId}");
    }

    public async Task<Guid> Add(ItemModel item)
    {
        ItemEntity itemEntity = item.ToEntity();

        return await _itemRepository.Add(itemEntity);
    }

    public async Task Update(ItemModel item)
    {
        await Get(item.Id, null);

        ItemEntity itemEntity = item.ToEntity();

        await _itemRepository.Update(itemEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id, null);

        await _itemRepository.Delete(id);
    }
}
