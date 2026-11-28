using Application.Interfaces;
using BillerContracts.Requests.Invoice;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice add validation
/// </summary>
public class InvoiceAddValidator : AbstractValidator<InvoiceAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public InvoiceAddValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Please specify user id");
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer id");
        RuleFor(x => x.Items).Must(x => x.Count != 0).WithMessage("Please provide at least one item");
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.CreatedDate).WithMessage("Please specify due date >= create date");

        RuleForEach(x => x.Items).SetValidator(new InvoiceItemValidator(documentTextChecker));

        RuleFor(x => x.Comments).DrawableOnPdf(documentTextChecker);
    }
}
