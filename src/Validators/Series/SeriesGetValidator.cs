using BillerContracts.Requests.Series;
using FluentValidation;

namespace Validators.Series;

/// <summary>
/// Series list query validation
/// </summary>
public class SeriesGetValidator : PagedRequestValidator<SeriesGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public SeriesGetValidator()
    {
        RuleFor(x => x.SellerId).NotEqual(Guid.Empty).WithMessage("Please specify valid seller Id");
    }
}
