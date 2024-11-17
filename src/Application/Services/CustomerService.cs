using Application.Interfaces;
using Application.Models;
using AutoMapper;
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
    private readonly IMapper _mapper;

    public CustomerService(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<CustomerModel> Get(Guid id)
    {
        CustomerEntity customerEntity = await _customerRepository.Get(id)
            ?? throw new NotFoundException($"Invoice customer:{id} not found");

        return _mapper.Map<CustomerModel>(customerEntity);
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

        return _mapper.Map<IEnumerable<CustomerModel>>(customerEntities);
    }

    public async Task<Result<CustomerModel>> GetWithValidation(Guid id, Guid sellerId)
    {
        var customer = await _customerRepository.Get(id);
        
        if (customer is null || customer.SellerId != sellerId)
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Customer id {id} is invalid for seller id {sellerId}" };

        return _mapper.Map<CustomerModel>(customer);
    }

    public async Task<Guid> Add(CustomerModel customer)
    {
        customer.InvoiceNumber = 1;
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        return await _customerRepository.Add(customerEntity);
    }

    public async Task Update(CustomerModel customer)
    {
        await Get(customer.Id);

        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

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
