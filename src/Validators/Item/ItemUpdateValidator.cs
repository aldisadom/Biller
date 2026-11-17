using Application.Interfaces;
using BillerContracts.Requests.Item;
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
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public ItemUpdateValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify item Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name");
        RuleFor(x => x.Price).GreaterThan(0.0M).WithMessage("Please provide price that must be > 0");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity")
            .GreaterThan(-1).WithMessage("Quantity can not be negative, except -1 quantity not used");

        RuleFor(x => x.Name).DrawableOnPdf(documentTextChecker);
    }
}
