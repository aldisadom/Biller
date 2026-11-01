using BillerContracts.Requests.Customer;
using FluentValidation;

namespace Validators.Customer;

/// <summary>
/// Customer list query validation
/// </summary>
public class CustomerGetValidator : AbstractValidator<CustomerGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public CustomerGetValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
