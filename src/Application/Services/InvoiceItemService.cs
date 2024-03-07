using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceItem;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;

namespace Application.Services;

public class InvoiceItemService : IInvoiceItemService
{
    private readonly IInvoiceItemRepository _invoiceItemEntityRepository;
    private readonly IMapper _mapper;

    public InvoiceItemService(IInvoiceItemRepository invoiceItemEntityRepository, IMapper mapper)
    {
        _invoiceItemEntityRepository = invoiceItemEntityRepository;
        _mapper = mapper;
    }

    public async Task<InvoiceItemModel> Get(Guid id)
    {
        InvoiceItemEntity invoiceItemEntity = await _invoiceItemEntityRepository.Get(id)
            ?? throw new NotFoundException($"Invoice invoiceItem:{id} not found");

        return _mapper.Map<InvoiceItemModel>(invoiceItemEntity);
    }

    public async Task<IEnumerable<InvoiceItemModel>> Get(InvoiceItemGetRequest query)
    {
        IEnumerable<InvoiceItemEntity> invoiceItemEntities;

        if (query is null)
            invoiceItemEntities = await _invoiceItemEntityRepository.Get();
        else if (query.AddressId is not null)
            invoiceItemEntities = await _invoiceItemEntityRepository.GetByUser((Guid)query.AddressId);
        else
            invoiceItemEntities = await _invoiceItemEntityRepository.Get();

        return _mapper.Map<IEnumerable<InvoiceItemModel>>(invoiceItemEntities);
    }

    public async Task<Guid> Add(InvoiceItemModel invoiceItem)
    {
        InvoiceItemEntity invoiceItemEntity = _mapper.Map<InvoiceItemEntity>(invoiceItem);

        return await _invoiceItemEntityRepository.Add(invoiceItemEntity);
    }

    public async Task Update(InvoiceItemModel invoiceItem)
    {
        await Get(invoiceItem.Id);

        InvoiceItemEntity invoiceItemEntity = _mapper.Map<InvoiceItemEntity>(invoiceItem);

        await _invoiceItemEntityRepository.Update(invoiceItemEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _invoiceItemEntityRepository.Delete(id);
    }
}