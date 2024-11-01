using Contracts.Requests.Customer;
using Contracts.Requests.Invoice;
using Contracts.Requests.Seller;
using Domain.Repositories;
using FluentValidation;
using Validators.Seller;

namespace Validators.Invoice;

/// <summary>
/// Invoice update validation
/// </summary>
public class InvoiceUpdateValidator : AbstractValidator<InvoiceUpdateRequest>
{
    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify user id");
        RuleFor(x => x.Seller).NotEmpty().WithMessage("Please specify seller");
        RuleFor(x => x.Customer).NotEmpty().WithMessage("Please specify customer");
        RuleFor(x => x.Items).Must(x => x.Count != 0).WithMessage("Please provide at least one item");
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.CreatedDate).WithMessage("Please specify due date >= create date");

        RuleFor(x => x.Items).Must(ValidateInvoiceItems);
        RuleFor(x => x.Seller).Must(ValidateInvoiceSeller);
        RuleFor(x => x.Customer).Must(ValidateInvoiceCustomer);
    }

    private bool ValidateInvoiceItems(List<InvoiceItemUpdateRequest> items)
    {
        InvoiceItemUpdateValidator validator = new();

        foreach (var item in items)
            validator.CheckValidation(item);

        return true;
    }

    private bool ValidateInvoiceSeller(SellerUpdateRequest seller)
    {
        new SellerUpdateValidator().CheckValidation(seller);

        return true;
    }

    private bool ValidateInvoiceCustomer(CustomerUpdateRequest customer)
    {
        new CustomerUpdateValidator().CheckValidation(customer);

        return true;
    }
}
