using BillerContracts.Requests.Item;
using FluentValidation;

namespace Validators.Item;

/// <summary>
/// Item list query validation
/// </summary>
public class ItemGetValidator : PagedRequestValidator<ItemGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public ItemGetValidator()
    {
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty).WithMessage("Please specify valid customer Id");
    }
}
