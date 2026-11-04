using Application.Models;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice model validation before update: every id is set and every item quantity is above 0
/// </summary>
public class InvoiceModelUpdateValidator : AbstractValidator<InvoiceModel>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceModelUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify invoice id");
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer id");
        RuleFor(x => x.Items).NotNull().WithMessage("Please provide at least one item");
        RuleForEach(x => x.Items).Must(i => i.Id != Guid.Empty).WithMessage("Please specify item Id");
        RuleForEach(x => x.Items).Must(i => i.Quantity > 0).WithMessage((_, i) => $"Please provide quantity that should > 0 for {i.Id}");
    }
}
