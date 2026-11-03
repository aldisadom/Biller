using BillerContracts.Requests.Customer;
using FluentValidation;

namespace Validators.Customer;

/// <summary>
/// Customer list query validation
/// </summary>
public class CustomerGetValidator : PagedRequestValidator<CustomerGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public CustomerGetValidator()
    {
        RuleFor(x => x.SellerId).NotEqual(Guid.Empty).WithMessage("Please specify valid seller Id");
    }
}
