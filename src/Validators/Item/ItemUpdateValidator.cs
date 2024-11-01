using Contracts.Requests.Item;
using FluentValidation;

namespace Validators.Item;

/// <summary>
/// Item update validation
/// </summary>

public class ItemUpdateValidator : AbstractValidator<ItemUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public ItemUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify item Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name");
        RuleFor(x => x.Price).GreaterThan(0.0M).WithMessage("Price should be more than zero");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity")
            .GreaterThan(-1).WithMessage("Quantity can not be negative, except -1 quantity not used");
    }
}
