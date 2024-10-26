using Contracts.Requests.Item;
using Contracts.Requests.User;
using FluentValidation;

namespace Contracts.Validations.Item;

/// <summary>
/// Item add validation
/// </summary>
public class ItemAddValidator : AbstractValidator<ItemAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public ItemAddValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify item name");
        RuleFor(x => x.Price).GreaterThan(0.0M).WithMessage("Price should be more than zero");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify item quantity")
            .GreaterThan(-1).WithMessage("Quantity can not be negative, except -1 quantity not used");
    }
}
