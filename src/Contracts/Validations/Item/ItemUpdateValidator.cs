using Contracts.Requests.Item;
using FluentValidation;

namespace Contracts.Validations.Item;

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
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please name of item");
        RuleFor(x => x.Price).NotEmpty().WithMessage("Please specify price of item")
            .GreaterThan(0).WithMessage("Price should be more than zero");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity of item")
            .GreaterThanOrEqualTo(-1).WithMessage("Quantity can not be negative, except -1 quantity not used");
    }
}
