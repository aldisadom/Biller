using Contracts.Requests.Seller;
using FluentValidation;

namespace Validators.Seller;

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
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state");
        RuleFor(x => x.Email).Must(EmailValidator.IsValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number");
        RuleFor(x => x.BankName).NotEmpty().WithMessage("Please provide bank name");
        RuleFor(x => x.BankNumber).NotEmpty().WithMessage("Please provide bank account number");
    }
}
