using Application.Interfaces;
using BillerContracts.Requests.Series;
using FluentValidation;

namespace Validators.Series;

/// <summary>
/// Series update validation
/// </summary>
public class SeriesUpdateValidator : AbstractValidator<SeriesUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    /// <param name="documentTextChecker">knows which characters the invoice PDF can show</param>
    public SeriesUpdateValidator(IDocumentTextChecker documentTextChecker)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify series Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name")
            .Must(name => name is null || name.Trim().Length <= 10).WithMessage("Please specify name of at most 10 characters");
        RuleFor(x => x.NextNumber).GreaterThan(0).WithMessage("Please specify next number that must be > 0")
            .LessThan(int.MaxValue).WithMessage("Please specify next number that must be < 2147483647");

        RuleFor(x => x.Name).DrawableOnPdf(documentTextChecker);
    }
}
