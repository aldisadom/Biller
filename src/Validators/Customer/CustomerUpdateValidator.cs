using Contracts.Requests.Customer;
using FluentValidation;
using Validators;

/// <summary>
/// Customer update validation
/// </summary>
public class CustomerUpdateValidator : AbstractValidator<CustomerUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public CustomerUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify customer Id");
        RuleFor(x => x.CompanyNumber).NotEmpty().WithMessage("Please specify company number");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Please specify company name");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state");
        RuleFor(x => x.Email).Must(EmailValidator.IsValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number");
        RuleFor(x => x.InvoiceName).NotEmpty().WithMessage("Please provide invoice name");
        RuleFor(x => x.InvoiceNumber).GreaterThan(0).WithMessage("Please provide invoice number that should be more than 0");
    }
}
