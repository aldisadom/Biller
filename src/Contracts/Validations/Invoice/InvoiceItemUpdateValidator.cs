using Contracts.Requests.Invoice;
using FluentValidation;

namespace Contracts.Validations.Invoice;

/// <summary>
/// Invoice item update validation
/// </summary>
public class InvoiceItemUpdateValidator : AbstractValidator<InvoiceItemUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceItemUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify item Id");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity of item")
            .GreaterThan(0).WithMessage("Quantity must be more than 0");

        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify item name");
        RuleFor(x => x.Price).NotEmpty().WithMessage("Please specify price of item")
            .GreaterThan(0).WithMessage("Price must be more than 0");
    }
}
