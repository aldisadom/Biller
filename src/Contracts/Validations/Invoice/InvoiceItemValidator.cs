using Contracts.Requests.Invoice;
using FluentValidation;

namespace Contracts.Validations.Invoice;

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
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity of item")
            .GreaterThan(0).WithMessage("Quantity must be more than 0");
    }
}
