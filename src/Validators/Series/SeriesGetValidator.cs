using BillerContracts.Enums;
using BillerContracts.Requests.Series;
using FluentValidation;

namespace Validators.Series;

/// <summary>
/// Series list query validation
/// </summary>
public class SeriesGetValidator : AbstractValidator<SeriesGetRequest>
{
    private static readonly int _maxPage = int.MaxValue / Enum.GetValues<PageSize>().Max(p => (int)p);

    /// <summary>
    /// Validation. The page limit keeps (page - 1) * page size inside an int for the largest page size.
    /// </summary>
    public SeriesGetValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, _maxPage).WithMessage($"Please specify page from 1 to {_maxPage}");
        RuleFor(x => x.PageSize).IsInEnum().WithMessage("Please specify valid page size");
    }
}
