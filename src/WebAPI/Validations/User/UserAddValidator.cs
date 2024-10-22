using Contracts.Requests.User;
using FluentValidation;

namespace WebAPI.Validations.User;

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
        RuleFor(x => x.Email).Must(BeValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Password).Must(BeValidPassword).WithMessage("Please specify a more complex password");
    }

    private bool BeValidEmail(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    private bool BeValidPassword(string password)
    {
        return PasswordAdvisor.CheckStrength(password) >= PasswordScore.VeryWeak;
    }
}
