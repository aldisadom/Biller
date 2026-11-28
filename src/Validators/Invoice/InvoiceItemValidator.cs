using Application.Interfaces;
using BillerContracts.Requests.Invoice;
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
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public InvoiceItemValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify item Id");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(x => $"Please provide quantity that should > 0 for {x.Id}");
        RuleFor(x => x.Version).GreaterThanOrEqualTo(1).When(x => x.Version is not null)
            .WithMessage(x => $"Please specify item version >= 1 for {x.Id}");

        RuleFor(x => x.Comments).DrawableOnPdf(documentTextChecker);
    }
}
