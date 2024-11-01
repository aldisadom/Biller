using Application.Models;
using Contracts.Requests.Item;
using Domain.Repositories;
using FluentValidation;

namespace Validators.Item;

/// <summary>
/// Item add validation
/// </summary>
public class ItemValidator : AbstractValidator<ItemModel>
{
    private readonly IItemRepository _item;

    /// <summary>
    /// Validation
    /// </summary>
    public ItemValidator(IItemRepository item)
    {
        _item = item;
    }

    public async Task<bool> IsValidItemsId(List<Guid> itemIds, Guid customer)
    {
        var items = await _item.GetByCustomerId(customer);
        return false;
        //return items.con(x => itemIds(y=> y == x.Id)).Count() == itemIds.Count;
    }

    public async Task<bool> IsValidItemId(Guid id)
    {
        var item = await _item.Get(id);
        return item is not null;
    }
}
