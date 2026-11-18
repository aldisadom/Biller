using Application.Interfaces;
using BillerContracts.Requests.Invoice;
using FluentValidation;
using FluentValidation.Results;

namespace Validators.Invoice;

/// <summary>
/// Invoice update validation
/// </summary>
public class InvoiceUpdateValidator : AbstractValidator<InvoiceUpdateRequest>
{
    private readonly IDocumentTextChecker _documentTextChecker;

    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public InvoiceUpdateValidator(IDocumentTextChecker documentTextChecker)
    {
        _documentTextChecker = documentTextChecker;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify invoice id");
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer id");
        RuleFor(x => x.Items).Must(x => x.Count != 0).WithMessage("Please provide at least one item");
        // A saved line keeps its version, so a save never re-prices it by accident
        RuleForEach(x => x.Items).Must(i => i.Version is not null).WithMessage((_, i) => $"Please specify item version for {i.Id}");
        RuleFor(x => x.CreatedDate).NotEmpty().WithMessage("Please specify create date");
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.CreatedDate).WithMessage("Please specify due date >= create date");

        RuleFor(x => x.Items).Must(ValidateInvoiceItems);

        RuleFor(x => x.Comments).DrawableOnPdf(documentTextChecker);
    }

    private bool ValidateInvoiceItems(List<InvoiceItemRequest> items)
    {
        InvoiceItemValidator validator = new(_documentTextChecker);

        string errorMessage = string.Empty;

        foreach (var item in items)
        {
            ValidationResult result = validator.Validate(item);
            if (!result.IsValid)
                errorMessage += (errorMessage.Length == 0 ? "" : "\n") + result.ToString();
        }

        if (string.IsNullOrEmpty(errorMessage))
            return true;
        else
            throw new ValidationException(errorMessage);
    }
}
