using BillerContracts.Requests;
using FluentValidation;

namespace Validators;

/// <summary>
/// Page size rule of every list query validator. Any page is accepted: the repositories answer a
/// page below 1 with page 1 and a page past the last one with the last page.
/// </summary>
public abstract class PagedRequestValidator<T> : AbstractValidator<T> where T : PagedRequest
{
    /// <summary>
    /// Validation
    /// </summary>
    protected PagedRequestValidator()
    {
        RuleFor(x => x.PageSize).IsInEnum().WithMessage("Please specify valid page size");
    }
}
