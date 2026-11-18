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

    public async Task<ItemModel> Get(Guid id, int version)
    {
        ItemEntity itemEntity = await _itemRepository.Get(id, version)
            ?? throw new NotFoundException($"Invoice item:{id} version {version} not found");

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

    public async Task<Result<IEnumerable<ItemVersionModel>>> GetVersionsWithValidation(List<InvoiceItemModel> lines, Guid customerId)
    {
        List<Guid> ids = lines.Select(l => l.Id).ToList();
        List<ItemVersionEntity> versions = (await _itemRepository.GetVersions(ids)).ToList();

        string idString = string.Empty;

        var nonMatching = ids
            .Where(id => !versions.Any(v => (v.ItemId == id) && (v.CustomerId == customerId)))
            .ToList();

        foreach (var item in nonMatching)
            idString += " " + item.ToString();

        if (!string.IsNullOrEmpty(idString))
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Items id{idString} is invalid for customer id {customerId}" };

        // An empty version means the latest, which always exists
        InvoiceItemModel? unknownVersion = lines.FirstOrDefault(l =>
            l.Version is not null && !versions.Any(v => (v.ItemId == l.Id) && (v.Version == l.Version)));

        if (unknownVersion is not null)
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Item id {unknownVersion.Id} has no version {unknownVersion.Version}" };

        return versions.Select(v => v.ToModel()).ToList();
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
