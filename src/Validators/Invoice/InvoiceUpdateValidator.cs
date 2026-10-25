using BillerContracts.Requests.Customer;
using BillerContracts.Requests.Invoice;
using BillerContracts.Requests.Seller;
using FluentValidation;
using Validators.Seller;

namespace Validators.Invoice;

/// <summary>
/// Invoice update validation
/// </summary>
public class InvoiceUpdateValidator : AbstractValidator<InvoiceUpdateRequest>
{
    private static readonly InvoiceItemUpdateValidator _itemValidator = new();
    private static readonly SellerUpdateValidator _sellerValidator = new();
    private static readonly CustomerUpdateValidator _customerValidator = CustomerUpdateValidator.ForInvoiceUpdate();

    /// <summary>
    /// Validation
    /// </summary>
    public InvoiceUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Please specify invoice id");
        RuleFor(x => x.Seller).NotEmpty().WithMessage("Please specify seller");
        RuleFor(x => x.Customer).NotEmpty().WithMessage("Please specify customer");
        RuleFor(x => x.Items).Must(x => x.Count != 0).WithMessage("Please provide at least one item");
        RuleFor(x => x.CreatedDate).NotEmpty().WithMessage("Please specify create date");
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.CreatedDate).WithMessage("Please specify due date >= create date");

        RuleFor(x => x.Items).Must(ValidateInvoiceItems);
        RuleFor(x => x.Seller).Must(ValidateInvoiceSeller);
        RuleFor(x => x.Customer).Must(ValidateInvoiceCustomer);
    }

    private bool ValidateInvoiceItems(List<InvoiceItemUpdateRequest> items)
    {
        foreach (var item in items)
            _itemValidator.CheckValidation(item);

        return true;
    }

    private bool ValidateInvoiceSeller(SellerUpdateRequest seller)
    {
        _sellerValidator.CheckValidation(seller);

        return true;
    }

    private bool ValidateInvoiceCustomer(CustomerUpdateRequest customer)
    {
        _customerValidator.CheckValidation(customer);

        return true;
    }
}
