﻿using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;

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
            ?? throw new NotFoundException($"Invoice client:{id} not found");

        return _mapper.Map<CustomerModel>(customerEntity);
    }

    public async Task<IEnumerable<CustomerModel>> Get(CustomerGetRequest query)
    {
        IEnumerable<CustomerEntity> customerEntities;

        if (query is null)
            customerEntities = await _customerRepository.Get();
        else if (query.UserId is not null)
            customerEntities = await _customerRepository.GetBySeller((Guid)query.UserId);
        else
            customerEntities = await _customerRepository.Get();

        return _mapper.Map<IEnumerable<CustomerModel>>(customerEntities);
    }

    public async Task<Guid> Add(CustomerModel customer)
    {
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        return await _customerRepository.Add(customerEntity);
    }

    public async Task Update(CustomerModel customer)
    {
        await Get(customer.Id);

        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        await _customerRepository.Update(customerEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _customerRepository.Delete(id);
    }
}
