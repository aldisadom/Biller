using BillerContracts.Requests.Series;
using FluentValidation;

namespace Validators.Series;

/// <summary>
/// Series list query validation
/// </summary>
public class SeriesGetValidator : AbstractValidator<SeriesGetRequest>
{
    /// <summary>
    /// Validation. The page limit keeps (page - 1) * page size inside an int for every page size.
    /// </summary>
    public SeriesGetValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, int.MaxValue / 50).WithMessage($"Please specify page from 1 to {int.MaxValue / 50}");
        RuleFor(x => x.PageSize).IsInEnum().WithMessage("Please specify valid page size");
    }
}
