using Contracts.Requests.User;
using FluentValidation;

namespace Contracts.Validations.User;

/// <summary>
/// User login validation
/// </summary>
public class UserLoginValidator : AbstractValidator<UserLoginRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public UserLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Please specify email");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Please specify a password");
    }
}
