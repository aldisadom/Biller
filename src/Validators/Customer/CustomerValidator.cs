using Application.Models;
using Contracts.Requests.Customer;
using Domain.Repositories;
using FluentValidation;

namespace Validators.Customer;

/// <summary>
/// Customer add validation
/// </summary>
public class CustomerValidator : AbstractValidator<CustomerModel>
{
    private readonly ICustomerRepository _customer;

    /// <summary>
    /// Validation
    /// </summary>
    public CustomerValidator(ICustomerRepository customer)
    {
        _customer = customer;
    }

    public async Task<bool> IsValidCustomerId(Guid customerId, Guid sellerId)
    {
        var customers = await _customer.GetBySellerId(sellerId);
        return customers.Where(x => x.Id == customerId).Count() == 1;
    }

    public async Task<bool> IsValidCustomerId(Guid id)
    {
        var customer = await _customer.Get(id);
        return customer is not null;
    }
}
