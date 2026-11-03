using BillerContracts.Enums;
using BillerContracts.Requests;
using FluentValidation;

namespace Validators;

/// <summary>
/// Paging limits shared by every list query validator
/// </summary>
public static class PagedRequestValidator
{
    /// <summary>
    /// Highest page. It keeps (page - 1) * page size inside an int for the largest page size.
    /// </summary>
    public static readonly int MaxPage = int.MaxValue / Enum.GetValues<PageSize>().Max(p => (int)p);
}

/// <summary>
/// Page and page size rules of every list query validator
/// </summary>
public abstract class PagedRequestValidator<T> : AbstractValidator<T> where T : PagedRequest
{
    /// <summary>
    /// Validation
    /// </summary>
    protected PagedRequestValidator()
    {
        int maxPage = PagedRequestValidator.MaxPage;
        RuleFor(x => x.Page).InclusiveBetween(1, maxPage).WithMessage($"Please specify page from 1 to {maxPage}");
        RuleFor(x => x.PageSize).IsInEnum().WithMessage("Please specify valid page size");
    }
}
