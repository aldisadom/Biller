using BillerContracts.Requests.Customer;
using FluentValidation;
using Validators;

/// <summary>
/// Customer update validation
/// </summary>
public class CustomerUpdateValidator : AbstractValidator<CustomerUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public CustomerUpdateValidator() : this(seriesRequired: true)
    {
    }

    /// <summary>
    /// Validation of the customer copy inside an invoice update. The invoice keeps its own series,
    /// so the customer's SeriesId isn't used and isn't required.
    /// </summary>
    public static CustomerUpdateValidator ForInvoiceUpdate() => new(seriesRequired: false);

    private CustomerUpdateValidator(bool seriesRequired)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify customer Id");
        RuleFor(x => x.CompanyNumber).NotEmpty().WithMessage("Please specify company number");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Please specify company name");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Please specify street");
        RuleFor(x => x.City).NotEmpty().WithMessage("Please specify city");
        RuleFor(x => x.State).NotEmpty().WithMessage("Please specify state");
        RuleFor(x => x.Email).Must(EmailValidator.IsValidEmail).WithMessage("Please provide valid email address");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Please provide phone number");
        if (seriesRequired)
            RuleFor(x => x.SeriesId).NotEmpty().WithMessage("Please specify series Id");
    }
}
