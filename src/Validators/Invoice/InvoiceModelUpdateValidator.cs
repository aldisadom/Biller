using Application.Interfaces;
using Application.Models;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice model validation before update: every id is set, every item quantity is above 0 and the PDF can show the comments
/// </summary>
public class InvoiceModelUpdateValidator : AbstractValidator<InvoiceModel>
{
    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public InvoiceModelUpdateValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify invoice id");
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer id");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Please provide at least one item");
        RuleForEach(x => x.Items).Must(i => i.Id != Guid.Empty).WithMessage("Please specify item Id");
        RuleForEach(x => x.Items).Must(i => i.Quantity > 0).WithMessage((_, i) => $"Please provide quantity that should > 0 for {i.Id}");

        RuleFor(x => x.Comments).DrawableOnPdf(documentTextChecker);
        RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(i => i.Comments).DrawableOnPdf(documentTextChecker));
    }
}
