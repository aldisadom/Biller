using BillerContracts.Requests.Invoice;
using FluentValidation;

namespace Validators.Invoice;

/// <summary>
/// Invoice list query validation
/// </summary>
public class InvoiceGetValidator : PagedRequestValidator<InvoiceGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceGetValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).WithMessage("Please specify valid user Id");
        RuleFor(x => x.SellerId).NotEqual(Guid.Empty).WithMessage("Please specify valid seller Id");
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty).WithMessage("Please specify valid customer Id");
    }
}
