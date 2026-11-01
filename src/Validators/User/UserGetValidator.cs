using BillerContracts.Requests.User;
using FluentValidation;

namespace Validators.User;

/// <summary>
/// User list query validation
/// </summary>
public class UserGetValidator : AbstractValidator<UserGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public UserGetValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
