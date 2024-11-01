using Application.Models;
using Contracts.Requests.Seller;
using Domain.Repositories;
using FluentValidation;

namespace Validators.Seller;

/// <summary>
/// Seller add validation
/// </summary>
public class SellerValidator : AbstractValidator<SellerModel>
{
    private readonly ISellerRepository _seller;

    /// <summary>
    /// Validation
    /// </summary>
    public SellerValidator(ISellerRepository seller)
    {
        _seller = seller;
    }

    public async Task<bool> IsValidSellerId(Guid sellerId, Guid userId)
    {
        var sellers = await _seller.GetByUserId(userId);
        return sellers.Where(x => x.Id == userId).Count() == 1;
    }

    public async Task<bool> IsValidSellerId(Guid id)
    {
        var seller = await _seller.Get(id);
        return seller is not null;
    }
}
