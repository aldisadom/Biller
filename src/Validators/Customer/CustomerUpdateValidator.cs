using Application.Interfaces;
using BillerContracts.Requests.Customer;
using FluentValidation;
using Validators;

/// <summary>
/// Customer update validation
/// </summary>
public class CustomerUpdateValidator : AbstractValidator<CustomerUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public CustomerUpdateValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify customer Id");
        RuleFor(x => x.CompanyNumber).NotEmpty().WithMessage("Please specify company number");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Please specify company name");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state");
        RuleFor(x => x.Email).Must(EmailValidator.IsValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number");
        RuleFor(x => x.SeriesId).NotEmpty().WithMessage("Please specify series Id");

        RuleFor(x => x.CompanyNumber).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.CompanyName).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.Street).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.City).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.Email).DrawableOnPdf(documentTextChecker);
        RuleFor(x => x.Phone).DrawableOnPdf(documentTextChecker);
    }
}
