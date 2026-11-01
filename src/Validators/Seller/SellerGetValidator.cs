using BillerContracts.Requests.Seller;
using FluentValidation;

namespace Validators.Seller;

/// <summary>
/// Seller list query validation
/// </summary>
public class SellerGetValidator : AbstractValidator<SellerGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public SellerGetValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
