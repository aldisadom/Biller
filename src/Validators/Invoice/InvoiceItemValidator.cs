using Contracts.Requests.Invoice;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice item validation
/// </summary>
public class InvoiceItemValidator : AbstractValidator<InvoiceItemRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify item Id");
        RuleFor(x => x.Quantity).GreaterThan(-1).WithMessage("Please provide quantity that should be more than zero, or -1 if quantity is not used");
    }
}
