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
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Please specify quantity");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Please provide price that must be > 0");
    }
}
