using BillerContracts.Requests.Seller;
using FluentValidation;

namespace Validators.Seller;

/// <summary>
/// Seller list query validation
/// </summary>
public class SellerGetValidator : PagedRequestValidator<SellerGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public SellerGetValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).WithMessage("Please specify valid user Id");
    }
}
