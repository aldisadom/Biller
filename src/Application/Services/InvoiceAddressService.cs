using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceAddress;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;

namespace Application.Services;



public class InvoiceAddressService : IInvoiceAddressService
{
    private readonly IInvoiceAddressRepository _invoiceAddressRepository;
    private readonly IMapper _mapper;

    public InvoiceAddressService(IInvoiceAddressRepository invoiceAddressRepository, IMapper mapper)
    {
        _invoiceAddressRepository = invoiceAddressRepository;
        _mapper = mapper;
    }

    public async Task<InvoiceAddressModel> Get(Guid id)
    {
        InvoiceAddressEntity invoiceAddressEntity = await _invoiceAddressRepository.Get(id)
            ?? throw new NotFoundException($"Invoice client:{id} not found");

        return _mapper.Map<InvoiceAddressModel>(invoiceAddressEntity);
    }

    public async Task<IEnumerable<InvoiceAddressModel>> Get(InvoiceAddressGetRequest query)
    {
        IEnumerable<InvoiceAddressEntity> invoiceAddressEntities;

        if (query is null)
            invoiceAddressEntities = await _invoiceAddressRepository.Get();
        else if (query.UserId is not null)
            invoiceAddressEntities = await _invoiceAddressRepository.GetByUser((Guid)query.UserId);
        else
            invoiceAddressEntities = await _invoiceAddressRepository.Get();

        return _mapper.Map<IEnumerable<InvoiceAddressModel>>(invoiceAddressEntities);
    }

    public async Task<Guid> Add(InvoiceAddressModel invoiceAddress)
    {
        InvoiceAddressEntity invoiceAddressEntity = _mapper.Map<InvoiceAddressEntity>(invoiceAddress);

        return await _invoiceAddressRepository.Add(invoiceAddressEntity);
    }

    public async Task Update(InvoiceAddressModel invoiceAddress)
    {
        await Get(invoiceAddress.Id);

        InvoiceAddressEntity invoiceAddressEntity = _mapper.Map<InvoiceAddressEntity>(invoiceAddress);

        await _invoiceAddressRepository.Update(invoiceAddressEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _invoiceAddressRepository.Delete(id);
    }
}
