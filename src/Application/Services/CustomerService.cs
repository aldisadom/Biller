using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Common;
using Contracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using System.Net;

namespace Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerModel> Get(Guid id)
    {
        CustomerEntity customerEntity = await _customerRepository.Get(id)
            ?? throw new NotFoundException($"Invoice customer:{id} not found");

        return customerEntity.ToModel();
    }

    public async Task<IEnumerable<CustomerModel>> Get(CustomerGetRequest? query)
    {
        IEnumerable<CustomerEntity> customerEntities;

        if (query is null)
            customerEntities = await _customerRepository.Get();
        else if (query.SellerId is not null)
            customerEntities = await _customerRepository.GetBySellerId((Guid)query.SellerId);
        else
            customerEntities = await _customerRepository.Get();

        return customerEntities.Select(c => c.ToModel());
    }

    public async Task<Result<CustomerModel>> GetWithValidation(Guid id, Guid sellerId)
    {
        var customer = await _customerRepository.Get(id);

        if (customer is null || customer.SellerId != sellerId)
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Customer id {id} is invalid for seller id {sellerId}" };

        return customer.ToModel();
    }

    public async Task<Guid> Add(CustomerModel customer)
    {
        customer.InvoiceNumber = 1;
        CustomerEntity customerEntity = customer.ToEntity();

        return await _customerRepository.Add(customerEntity);
    }

    public async Task Update(CustomerModel customer)
    {
        await Get(customer.Id);

        CustomerEntity customerEntity = customer.ToEntity();

        await _customerRepository.Update(customerEntity);
    }

    public async Task IncreaseInvoiceNumber(Guid id)
    {
        await _customerRepository.IncreaseInvoiceNumber(id);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _customerRepository.Delete(id);
    }
}
