using Application.Interfaces;
using BillerContracts.Requests.Invoice;
using FluentValidation;
using FluentValidation.Results;

namespace Validators.Invoice;

/// <summary>
/// Invoice add validation
/// </summary>
public class InvoiceAddValidator : AbstractValidator<InvoiceAddRequest>
{
    private readonly IDocumentTextChecker _documentTextChecker;

    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public InvoiceAddValidator(IDocumentTextChecker documentTextChecker)
    {
        _documentTextChecker = documentTextChecker;

        RuleFor(x => x.UserId).NotEmpty().WithMessage("Please specify user id");
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer id");
        RuleFor(x => x.Items).Must(x => x.Count != 0).WithMessage("Please provide at least one item");
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
