using Application.Interfaces;
using BillerContracts.Requests.User;
using FluentValidation;

namespace Validators.User;

/// <summary>
/// User add validation
/// </summary>
public class UserAddValidator : AbstractValidator<UserAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public UserAddValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Please specify last name");
        RuleFor(x => x.Email).Must(EmailValidator.IsValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Password).Must(BeValidPassword).WithMessage("Please specify a more complex password");

        RuleFor(x => x.Name).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.LastName).DrawableOnPdf(documentTextChecker);
    }

    private bool BeValidPassword(string password)
    {
        return PasswordAdvisor.CheckStrength(password) >= PasswordScore.VeryWeak;
    }
}
