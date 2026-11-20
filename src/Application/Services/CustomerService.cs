using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;

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

    public async Task<(IEnumerable<CustomerModel> Items, int TotalCount, int Page)> Get(CustomerGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount, returnedPage) = await _customerRepository.Get(query?.SellerId, page, (int)pageSize);

        return (entities.Select(c => c.ToModel()), totalCount, returnedPage);
    }

    public async Task<CustomerModel> GetForSeller(Guid id, Guid sellerId)
    {
        CustomerEntity? customer = await _customerRepository.Get(id);

        if (customer is null || customer.SellerId != sellerId)
            throw new ValidationException($"Customer id {id} is invalid for seller id {sellerId}");

        return customer.ToModel();
    }

    public async Task<Guid> Add(CustomerModel customer)
    {
        await _seriesService.GetForSeller(customer.SeriesId, customer.SellerId);

        CustomerEntity customerEntity = customer.ToEntity();

        return await _customerRepository.Add(customerEntity);
    }

    public async Task Update(CustomerModel customer)
    {
        CustomerModel stored = await Get(customer.Id);
        await _seriesService.GetForSeller(customer.SeriesId, stored.SellerId);

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
}
