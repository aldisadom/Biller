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
            .Must(name => name.Trim().Length <= 10).WithMessage("Please specify name of at most 10 characters");
    }
}
