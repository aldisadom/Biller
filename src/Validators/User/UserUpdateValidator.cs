using Application.Interfaces;
using BillerContracts.Requests.User;
using FluentValidation;

namespace Validators.User;

/// <summary>
/// User update validation
/// </summary>
public class UserUpdateValidator : AbstractValidator<UserUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public UserUpdateValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify id of user");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify a name");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Please specify a last name");

        RuleFor(x => x.Name).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.LastName).DrawableOnPdf(documentTextChecker);
    }
}
