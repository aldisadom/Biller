using Application.Models;
using Domain.Repositories;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice update validation
/// </summary>
public class InvoiceValidator : AbstractValidator<InvoiceModel>
{
    private readonly ICustomerRepository _customer;
    private readonly ISellerRepository _seller;
    private readonly IItemRepository _item;

    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceValidator(ICustomerRepository customer, ISellerRepository seller, IItemRepository item)
    {
        _customer = customer;
        _seller = seller;
        _item = item;
    }

    public async Task<bool> IsValidSellerId(Guid sellerId, Guid userId)
    {
        var sellers = await _seller.GetByUserId(userId);
        return sellers.Where(x => x.Id == sellerId).Count() == 1;
    }

    public async Task<bool> IsValidCustomerId(Guid customerId, Guid sellerId)
    {
        var customers = await _customer.GetBySellerId(sellerId);
        return customers.Where(x => x.Id == customerId).Count() == 1;
    }

    public async Task<bool> IsValidItemsId(List<Guid> itemIds, Guid customer)
    {
        var items = await _item.GetByCustomerId(customer);
        return false;
        //return items.con(x => itemIds(y=> y == x.Id)).Count() == itemIds.Count;

    }
}
