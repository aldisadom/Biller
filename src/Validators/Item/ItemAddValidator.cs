using Contracts.Requests.Item;
using FluentValidation;

namespace Validators.Item;

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
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name");
        RuleFor(x => x.Price).GreaterThan(0.0M).WithMessage("Please provide price that must be > 0");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity")
            .GreaterThan(-1).WithMessage("Quantity can not be negative, except -1 quantity not used");
    }
}
