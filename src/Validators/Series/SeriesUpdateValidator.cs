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
    public SeriesUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify series Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name")
            .Must(name => name.Trim().Length <= 10).WithMessage("Please specify name of at most 10 characters");
        RuleFor(x => x.NextNumber).GreaterThan(0).WithMessage("Please specify next number that must be > 0");
    }
}
