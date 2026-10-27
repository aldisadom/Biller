using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts;
using BillerContracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;
using System.Net;

namespace Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISeriesService _seriesService;

    public CustomerService(ICustomerRepository customerRepository, ISeriesService seriesService)
    {
        _customerRepository = customerRepository;
        _seriesService = seriesService;
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
        await ValidateSeries(customer.SeriesId, customer.SellerId);

        CustomerEntity customerEntity = customer.ToEntity();

        return await _customerRepository.Add(customerEntity);
    }

    public async Task Update(CustomerModel customer)
    {
        CustomerModel stored = await Get(customer.Id);
        await ValidateSeries(customer.SeriesId, stored.SellerId);

        CustomerEntity customerEntity = customer.ToEntity();

        bool updated = await _customerRepository.Update(customerEntity)
            ?? throw new NotFoundException($"Invoice customer:{customer.Id} not found");
        if (!updated)
            throw new ValidationException($"Customer id {customer.Id} has invoices, so its series can't be changed");
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _customerRepository.Delete(id);
    }

    private async Task ValidateSeries(Guid seriesId, Guid sellerId)
    {
        var seriesResult = await _seriesService.GetWithValidation(seriesId, sellerId);
        seriesResult.Match(
            series => { return series; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );
    }
}
