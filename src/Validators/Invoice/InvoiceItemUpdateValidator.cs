using Contracts.Requests.Invoice;
using FluentValidation;

namespace Validators.Invoice;

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
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify item name");
        RuleFor(x => x.Quantity).GreaterThan(-1).WithMessage("Please provide quantity that should be more than zero, or -1 if quantity is not used");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Please provide price that must be more than 0");
    }
}
