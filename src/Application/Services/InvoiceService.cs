using Application.Interfaces;
using Application.Models;
using Application.Models.InvoiceGenerationModels;
using AutoMapper;
using Contracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IMapper _mapper;
    private readonly ICustomerService _customerService;
    private readonly ISellerService _sellerService;
    private readonly IItemService _itemService;
    private readonly IUserService _userService;
    private readonly IInvoiceRepository _invoiceRepository;

    private static DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    private static DocumentSettings GetSettings() => DocumentSettings.Default;

    public InvoiceService(IMapper mapper, IUserService userService, ICustomerService CustomerService,
                        IItemService itemService, ISellerService sellerService, IInvoiceRepository invoiceRepository)
    {
        _mapper = mapper;
        _userService = userService;
        _itemService = itemService;
        _customerService = CustomerService;
        _sellerService = sellerService;
        _invoiceRepository = invoiceRepository;
    }

    private static void GenerateInvoiceFolderPath(InvoiceModel invoiceData)
    {
        string folderPath = invoiceData.GenerateFolderLocation();
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    private async Task GetInvoiceDetails(InvoiceModel invoiceData)
    {
        invoiceData.User = await _userService.Get(invoiceData.User!.Id);
        invoiceData.Seller = await _sellerService.Get(invoiceData.Seller!.Id);
        invoiceData.Customer = await _customerService.Get(invoiceData.Customer!.Id);
        invoiceData.InvoiceNumber = invoiceData.Customer.InvoiceNumber;
        List<ItemModel> items = (await _itemService.Get(invoiceData.Items!.Select(x => x.Id).ToList())).ToList();

        MapItemToInvoiceItem(invoiceData.Items!, items);

        GenerateInvoiceFolderPath(invoiceData);
    }

    public void MapInvoiceItemToItem(List<InvoiceItemModel> invoiceItems, List<ItemModel> items)
    {
        foreach (ItemModel item in items)
        {
            foreach (InvoiceItemModel invoiceItem in invoiceItems)
            {
                if (invoiceItem.Id == item.Id)
                {
                    item.Price = invoiceItem.Price;
                    item.Name = invoiceItem.Name;
                    break;
                }
            }
        }
    }

    public void MapItemToInvoiceItem(List<InvoiceItemModel> invoiceItems, List<ItemModel> items)
    {
        foreach (InvoiceItemModel invoiceItem in invoiceItems)
        {
            foreach (ItemModel item in items)
            {
                if (invoiceItem.Id == item.Id)
                {
                    invoiceItem.Price = item.Price;
                    invoiceItem.Name = item.Name;
                    break;
                }
            }
        }
    }

    public async Task<InvoiceModel> Get(Guid id)
    {
        InvoiceEntity invoiceDataEntity = await _invoiceRepository.Get(id)
            ?? throw new NotFoundException($"Invoice data:{id} not found");

        return _mapper.Map<InvoiceModel>(invoiceDataEntity);
    }

    public async Task<IEnumerable<InvoiceModel>> Get(InvoiceGetRequest query)
    {
        IEnumerable<InvoiceEntity> invoiceDataEntities;

        if (query is null)
            invoiceDataEntities = await _invoiceRepository.Get();
        else if (query.CustomerId is not null)
            invoiceDataEntities = await _invoiceRepository.GetByCustomerId((Guid)query.CustomerId);
        else if (query.SellerId is not null)
            invoiceDataEntities = await _invoiceRepository.GetBySellerId((Guid)query.SellerId);
        else if (query.UserId is not null)
            invoiceDataEntities = await _invoiceRepository.GetByUserId((Guid)query.UserId);
        else
            invoiceDataEntities = await _invoiceRepository.Get();

        return invoiceDataEntities.Select(i => _mapper.Map<InvoiceModel>(i));
    }

    public async Task<Guid> Add(InvoiceModel invoiceData)
    {
        await GetInvoiceDetails(invoiceData);

        InvoiceEntity invoiceDataEntity = _mapper.Map<InvoiceEntity>(invoiceData);

        Guid id = await _invoiceRepository.Add(invoiceDataEntity);
        await _customerService.UpdateInvoiceNumber(invoiceData.Customer!.Id);

        return id;
    }

    public async Task Update(InvoiceModel invoiceData)
    {
        await Get(invoiceData.Id);

        InvoiceEntity invoiceEntity = _mapper.Map<InvoiceEntity>(invoiceData);

        await _invoiceRepository.Update(invoiceEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _invoiceRepository.Delete(id);
    }

    public async Task GeneratePDF(Guid id)
    {
        InvoiceModel invoiceData = await Get(id);
        InvoiceDocument document = new(invoiceData);
        document.GeneratePdf(invoiceData.GenerateFileLocation());

        InvoiceDocumentADA documentADA = new(invoiceData);
        documentADA.GeneratePdf(invoiceData.GenerateFileLocation("ADA"));
    }
}
