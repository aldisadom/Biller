using Common.Validators;
using Contracts.Requests.Customer;
using Contracts.Requests.Seller;
using FluentValidation;
using FluentValidation.Validators;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Validations.Customer;

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
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street of customer");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city of customer");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state of customer");
        RuleFor(x => x.Email).Must(EmailValidator.BeValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number of customer");
        RuleFor(x => x.InvoiceName).NotEmpty().WithMessage("Please provide invoice name that will be used for customer");
    }
}
