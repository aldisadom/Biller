using Contracts.Requests.Invoice;
using FluentValidation;

namespace Contracts.Validations.Invoice;

/// <summary>
/// Invoice add validation
/// </summary>
public class InvoiceAddValidator : AbstractValidator<InvoiceAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceAddValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Please specify user id");
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer id");
        RuleFor(x => x.Items).NotEmpty().Must(x => x.Count != 0).WithMessage("Please provide at least one item");
        RuleFor(x => x.DueDate).NotEmpty().WithMessage("Please specify due date");

        RuleFor(x => x.Items).Must(ValidateInvoiceItems);
    }

    private bool ValidateInvoiceItems(List<InvoiceItemRequest> items)
    {
        InvoiceItemValidator validator = new();

        foreach (var item in items)
            validator.CheckValidation(item);

        return true;
    }
}
