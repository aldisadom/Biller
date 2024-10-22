using Contracts.Requests.User;
using FluentValidation;

namespace WebAPI.Validations.User;

/// <summary>
/// User update validation
/// </summary>
public class UserUpdateValidator : AbstractValidator<UserUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public UserUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify id of user");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify a last name");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Please specify a last name");
    }
}
