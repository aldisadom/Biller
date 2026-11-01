using BillerContracts.Requests.Series;
using FluentValidation;

namespace Validators.Series;

/// <summary>
/// Series list query validation
/// </summary>
public class SeriesGetValidator : AbstractValidator<SeriesGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public SeriesGetValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
