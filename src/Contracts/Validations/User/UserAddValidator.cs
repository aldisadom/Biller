using Contracts.Requests.User;
using FluentValidation;

namespace Contracts.Validations.User;

/// <summary>
/// User add validation
/// </summary>
public class UserAddValidator : AbstractValidator<UserAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public UserAddValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Please specify last name");
        RuleFor(x => x.Email).Must(EmailValidator.BeValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Password).Must(BeValidPassword).WithMessage("Please specify a more complex password");
    }

    private bool BeValidPassword(string password)
    {
        return PasswordAdvisor.CheckStrength(password) >= PasswordScore.VeryWeak;
    }
}
