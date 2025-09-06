using Application.Interfaces;
using Application.Models;
using Application.Models.InvoiceGenerationModels;
using AutoMapper;
using Common.Enums;
using Contracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IMapper _mapper;
    private readonly ICustomerService _customerService;
    private readonly ISellerService _sellerService;
    private readonly IItemService _itemService;
    private readonly IUserService _userService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceDocumentFactory _invoiceDocumentFactory;

    public InvoiceService(IMapper mapper, IUserService userService, ICustomerService CustomerService,
                        IItemService itemService, ISellerService sellerService, IInvoiceRepository invoiceRepository, IInvoiceDocumentFactory invoiceDocumentFactory)
    {
        _mapper = mapper;
        _userService = userService;
        _itemService = itemService;
        _customerService = CustomerService;
        _sellerService = sellerService;
        _invoiceRepository = invoiceRepository;
        _invoiceDocumentFactory = invoiceDocumentFactory;
    }

    private static void GenerateInvoiceFolderPath(InvoiceModel invoiceData)
    {
        string folderPath = invoiceData.GenerateFolderLocation();
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    private async Task GetInvoiceData(InvoiceModel invoiceData)
    {
        invoiceData.User = await _userService.Get(invoiceData.User!.Id);

        var (seller, customer, items) = await Validate(invoiceData);
        invoiceData.Seller = seller;
        invoiceData.Customer = customer;
        invoiceData.InvoiceNumber = invoiceData.Customer.InvoiceNumber;

        MapItemToInvoiceItem(invoiceData.Items!, items);

        GenerateInvoiceFolderPath(invoiceData);
    }

    public static void MapInvoiceItemToItem(List<InvoiceItemModel> invoiceItems, List<ItemModel> items)
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

    public static void MapItemToInvoiceItem(List<InvoiceItemModel> invoiceItems, List<ItemModel> items)
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
            ?? throw new NotFoundException($"Invoice:{id} not found");

        return _mapper.Map<InvoiceModel>(invoiceDataEntity);
    }

    public async Task<IEnumerable<InvoiceModel>> Get(InvoiceGetRequest? query)
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
        await GetInvoiceData(invoiceData);

        InvoiceEntity invoiceDataEntity = _mapper.Map<InvoiceEntity>(invoiceData);

        Guid id = await _invoiceRepository.Add(invoiceDataEntity);
        await _customerService.IncreaseInvoiceNumber(invoiceData.Customer!.Id);

        return id;
    }

    public async Task Update(InvoiceModel invoiceData)
    {
        var invoice = await Get(invoiceData.Id);
        invoiceData.User = invoice.User;
        await Validate(invoiceData);

        InvoiceEntity invoiceEntity = _mapper.Map<InvoiceEntity>(invoiceData);

        await _invoiceRepository.Update(invoiceEntity);
    }

    private async Task<(SellerModel seller, CustomerModel customer, List<ItemModel> items)> Validate(InvoiceModel invoiceData)
    {
        var sellerResult = await _sellerService.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id);
        var seller = sellerResult.Match(
            seller => { return seller; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );
        var customerResult = await _customerService.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id);
        var customer = customerResult.Match(
            customer => { return customer; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );
        var itemsResult = await _itemService.GetWithValidation(invoiceData.Items!.Select(x => x.Id).ToList(), invoiceData.Customer!.Id);
        var items = itemsResult.Match(
            items => { return items; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );

        return (seller, customer, items);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _invoiceRepository.Delete(id);
    }

    public async Task<FileStream> GeneratePDF(Guid id, Language languageCode, DocumentType documentType)
    {
        InvoiceModel invoiceData = await Get(id);
        var path = _invoiceDocumentFactory.GeneratePdf(documentType, languageCode, invoiceData);

        return File.OpenRead(path);
    }
}
