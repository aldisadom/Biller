using Contracts.Requests.Customer;
using FluentValidation;

namespace Validators.Customer;

/// <summary>
/// Customer add validation
/// </summary>
public class CustomerAddValidator : AbstractValidator<CustomerAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public CustomerAddValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.CompanyNumber).NotEmpty().WithMessage("Please specify company number");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Please specify company name");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state");
        RuleFor(x => x.Email).Must(EmailValidator.IsValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number");
        RuleFor(x => x.InvoiceName).NotEmpty().WithMessage("Please provide invoice name");
    }
}
