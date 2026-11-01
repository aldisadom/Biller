using BillerContracts.Requests.Item;
using FluentValidation;

namespace Validators.Item;

/// <summary>
/// Item list query validation
/// </summary>
public class ItemGetValidator : AbstractValidator<ItemGetRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public ItemGetValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
