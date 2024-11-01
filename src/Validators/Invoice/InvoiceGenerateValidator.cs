using Contracts.Requests.Invoice;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice generate validation
/// </summary>
public class InvoiceGenerateValidator : AbstractValidator<InvoiceGenerateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceGenerateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify invoice Id");
    }
}
