using BillerContracts.Requests.Series;
using FluentValidation;

namespace Validators.Series;

/// <summary>
/// Series add validation
/// </summary>
public class SeriesAddValidator : AbstractValidator<SeriesAddRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public SeriesAddValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty().WithMessage("Please specify seller Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please specify name")
            .MaximumLength(10).WithMessage("Please specify name of at most 10 characters");
        RuleFor(x => x.NextNumber).GreaterThan(0).When(x => x.NextNumber is not null).WithMessage("Please specify next number that must be > 0");
    }
}
