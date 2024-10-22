using Common.Validators;
using Contracts.Requests.Seller;
using FluentValidation;
using FluentValidation.Validators;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Validations.Seller;

/// <summary>
/// Seller add validation
/// </summary>
public class SellerAddValidator : AbstractValidator<SellerAddRequest>
{    
    /// <summary>
    /// Validation
    /// </summary>
    public SellerAddValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Please specify user Id");
        RuleFor(x => x.CompanyNumber).NotEmpty().WithMessage("Please specify company number");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Please specify company name");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street of company");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city of company");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state of company");
        RuleFor(x => x.Email).Must(EmailValidator.BeValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number of company");
        RuleFor(x => x.BankName).NotEmpty().WithMessage("Please provide bank name that uses this company");
        RuleFor(x => x.BankNumber).NotEmpty().WithMessage("Please provide bank account number that uses this company");
    }
}
