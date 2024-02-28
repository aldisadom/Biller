using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceClient;
using Contracts.Requests.InvoiceItem;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;

namespace Application.Services;



public class InvoiceClientService : IInvoiceClientService
{
    private readonly IInvoiceClientRepository _invoiceClientRepository;
    private readonly IMapper _mapper;

    public InvoiceClientService(IInvoiceClientRepository invoiceClientRepository, IMapper mapper)
    {
        _invoiceClientRepository = invoiceClientRepository;
        _mapper = mapper;
    }

    public async Task<InvoiceClientModel> Get(Guid id)
    {
        InvoiceClientEntity invoiceClientEntity = await _invoiceClientRepository.Get(id)
            ?? throw new NotFoundException($"Invoice client:{id} not found");

        return _mapper.Map<InvoiceClientModel>(invoiceClientEntity);
    }

    public async Task<IEnumerable<InvoiceClientModel>> Get(InvoiceClientGetRequest query)
    {
        IEnumerable<InvoiceClientEntity> invoiceClientEntities;

        if (query is null)
            invoiceClientEntities = await _invoiceClientRepository.Get();
        else if (query.UserId is not null)
            invoiceClientEntities = await _invoiceClientRepository.GetByUser((Guid)query.UserId);
        else
            invoiceClientEntities = await _invoiceClientRepository.Get();

        return _mapper.Map<IEnumerable<InvoiceClientModel>>(invoiceClientEntities);
    }

    public async Task<Guid> Add(InvoiceClientModel invoiceClient)
    {
        InvoiceClientEntity invoiceClientEntity = _mapper.Map<InvoiceClientEntity>(invoiceClient);

        return await _invoiceClientRepository.Add(invoiceClientEntity);
    }

    public async Task Update(InvoiceClientModel invoiceClient)
    {
        await Get(invoiceClient.Id);

        InvoiceClientEntity invoiceClientEntity = _mapper.Map<InvoiceClientEntity>(invoiceClient);

        await _invoiceClientRepository.Update(invoiceClientEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _invoiceClientRepository.Delete(id);
    }
}
