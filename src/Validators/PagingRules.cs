using BillerContracts.Enums;
using FluentValidation;

namespace Validators;

/// <summary>
/// Page and page size rules shared by the list query validators
/// </summary>
public static class PagingRules
{
    /// <summary>
    /// Highest page. It keeps (page - 1) * page size inside an int for the largest page size.
    /// </summary>
    public static readonly int MaxPage = int.MaxValue / Enum.GetValues<PageSize>().Max(p => (int)p);

    /// <summary>
    /// Page from 1 to <see cref="MaxPage"/>
    /// </summary>
    public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder.InclusiveBetween(1, MaxPage).WithMessage($"Please specify page from 1 to {MaxPage}");
    }

    /// <summary>
    /// One of the defined page sizes
    /// </summary>
    public static IRuleBuilderOptions<T, PageSize> ValidPageSize<T>(this IRuleBuilder<T, PageSize> ruleBuilder)
    {
        return ruleBuilder.IsInEnum().WithMessage("Please specify valid page size");
    }
}
