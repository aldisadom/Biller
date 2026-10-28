using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ICustomerService _customerService;
    private readonly ISellerService _sellerService;
    private readonly IItemService _itemService;
    private readonly IUserService _userService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceDocumentFactory _invoiceDocumentFactory;

    public InvoiceService(IUserService userService, ICustomerService CustomerService,
                        IItemService itemService, ISellerService sellerService, IInvoiceRepository invoiceRepository, IInvoiceDocumentFactory invoiceDocumentFactory)
    {
        _userService = userService;
        _itemService = itemService;
        _customerService = CustomerService;
        _sellerService = sellerService;
        _invoiceRepository = invoiceRepository;
        _invoiceDocumentFactory = invoiceDocumentFactory;
    }

    public static void MapItemToInvoiceItem(List<InvoiceItemModel> invoiceItems, IEnumerable<ItemModel> items)
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

    private async Task<InvoiceModel> GetStored(Guid id)
    {
        InvoiceEntity invoiceDataEntity = await _invoiceRepository.Get(id)
            ?? throw new NotFoundException($"Invoice:{id} not found");

        return invoiceDataEntity.ToModel();
    }

    public async Task<InvoiceModelFull> Get(Guid id)
    {
        InvoiceModel invoice = await GetStored(id);

        UserModel user = await _userService.Get(invoice.UserId);
        SellerModel seller = await _sellerService.Get(invoice.SellerId);
        CustomerModel customer = await _customerService.Get(invoice.CustomerId);

        return invoice.ToFull(user, seller, customer);
    }

    public async Task<(IEnumerable<InvoiceModelFull>, int)> Get(InvoiceGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount) = await _invoiceRepository.Get(
            query?.UserId, query?.SellerId, query?.CustomerId, page, (int)pageSize);

        List<InvoiceModel> invoices = entities.Select(i => i.ToModel()).ToList();

        Dictionary<Guid, UserModel> users = await GetEach(invoices.Select(i => i.UserId), _userService.Get);
        Dictionary<Guid, SellerModel> sellers = await GetEach(invoices.Select(i => i.SellerId), _sellerService.Get);
        Dictionary<Guid, CustomerModel> customers = await GetEach(invoices.Select(i => i.CustomerId), _customerService.Get);

        return (invoices.Select(i => i.ToFull(users[i.UserId], sellers[i.SellerId], customers[i.CustomerId])).ToList(), totalCount);
    }

    private static async Task<Dictionary<Guid, T>> GetEach<T>(IEnumerable<Guid> ids, Func<Guid, Task<T>> get)
    {
        Dictionary<Guid, T> result = [];
        foreach (Guid id in ids.Distinct())
            result[id] = await get(id);

        return result;
    }

    public async Task<Guid> Add(InvoiceModel invoiceData)
    {
        await _userService.Get(invoiceData.UserId);

        var (customer, items) = await Validate(invoiceData);
        invoiceData.InvoiceNumber = customer.InvoiceNumber;
        invoiceData.Status = InvoiceStatus.Created;
        MapItemToInvoiceItem(invoiceData.Items!, items);

        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity(invoiceData.GenerateFileLocation(customer.InvoiceName));

        Guid id = await _invoiceRepository.Add(invoiceDataEntity);
        await _customerService.IncreaseInvoiceNumber(invoiceData.CustomerId);

        return id;
    }

    public async Task Update(InvoiceModel invoiceData)
    {
        InvoiceModel invoice = await GetStored(invoiceData.Id);
        invoiceData.UserId = invoice.UserId;
        invoiceData.Status = invoice.Status;

        var (customer, items) = await Validate(invoiceData);
        MapItemToInvoiceItem(invoiceData.Items!, items);

        InvoiceEntity invoiceEntity = invoiceData.ToEntity(invoiceData.GenerateFileLocation(customer.InvoiceName));

        await _invoiceRepository.Update(invoiceEntity);
    }

    public async Task UpdateStatus(InvoiceUpdateStatusRequest invoiceData)
    {
        if (!Enum.IsDefined(typeof(InvoiceStatus), invoiceData.Status))
            throw new ValidationException($"Invalid invoice status: {invoiceData.Status}");

        if (invoiceData.Status == InvoiceStatus.OverDue)
            throw new ValidationException("Overdue status cannot be set manually");

        await GetStored(invoiceData.Id);

        await _invoiceRepository.UpdateStatus(invoiceData.Id, invoiceData.Status);
    }

    private async Task<(CustomerModel customer, IEnumerable<ItemModel> items)> Validate(InvoiceModel invoiceData)
    {
        var sellerResult = await _sellerService.GetWithValidation(invoiceData.SellerId, invoiceData.UserId);
        sellerResult.Match(
            seller => { return seller; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );
        var customerResult = await _customerService.GetWithValidation(invoiceData.CustomerId, invoiceData.SellerId);
        var customer = customerResult.Match(
            customer => { return customer; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );
        var itemsResult = await _itemService.GetWithValidation(invoiceData.Items!.Select(x => x.Id).ToList(), invoiceData.CustomerId);
        var items = itemsResult.Match(
            items => { return items; },
            error => { throw new ValidationException(error.ExtendedMessage); }
        );

        return (customer, items);
    }

    public async Task Delete(Guid id)
    {
        await GetStored(id);

        await _invoiceRepository.Delete(id);
    }

    public async Task<(MemoryStream, string)> GeneratePDF(Guid id, Language languageCode, DocumentType documentType)
    {
        if (!Enum.IsDefined(typeof(Language), languageCode))
            throw new ValidationException($"Invalid language code: {languageCode}");

        if (!Enum.IsDefined(typeof(DocumentType), documentType))
            throw new ValidationException($"Invalid document type: {documentType}");

        InvoiceModelFull invoiceData = await Get(id);
        return (_invoiceDocumentFactory.GeneratePdf(documentType, languageCode, invoiceData), invoiceData.GenerateFileLocation());
    }
}
