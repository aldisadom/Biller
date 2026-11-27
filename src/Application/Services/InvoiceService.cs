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

    public async Task<(IEnumerable<InvoiceModelFull> Invoices, int TotalCount, int Page)> Get(InvoiceGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (invoiceEntities, totalCount, returnedPage) = await _invoiceRepository.Get(
            query?.UserId, query?.SellerId, query?.CustomerId, page, (int)pageSize);

        List<InvoiceModel> invoices = invoiceEntities.Select(i => i.ToModel()).ToList();

        Dictionary<Guid, UserModel> users = await GetEach(invoices.Select(i => i.UserId), _userService.Get);
        Dictionary<Guid, SellerModel> sellers = await GetEach(invoices.Select(i => i.SellerId), _sellerService.Get);
        Dictionary<Guid, CustomerModel> customers = await GetEach(invoices.Select(i => i.CustomerId), _customerService.Get);

        return (invoices.Select(i => i.ToFull(users[i.UserId], sellers[i.SellerId], customers[i.CustomerId])).ToList(), totalCount, returnedPage);
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

        CustomerModel customer = await Validate(invoiceData);
        invoiceData.SeriesId = customer.SeriesId;
        invoiceData.Status = InvoiceStatus.Created;

        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity();

        Guid id = await _invoiceRepository.Add(invoiceDataEntity);
        if (id == Guid.Empty)
            throw new ValidationException($"Invoice was not created: customer id {invoiceData.CustomerId} is no longer in series id {invoiceData.SeriesId}, or the series has no numbers left");

        return id;
    }

    public async Task Update(InvoiceModel invoiceData)
    {
        InvoiceModel invoice = await GetStored(invoiceData.Id);

        // The invoice keeps its series and number, and a series belongs to one seller.
        if (invoiceData.SellerId != invoice.SellerId)
            throw new ValidationException($"Invoice id {invoiceData.Id} can't be moved to another seller");

        if (invoiceData.CustomerId != invoice.CustomerId)
            throw new ValidationException($"Invoice id {invoiceData.Id} can't be moved to another customer");

        invoiceData.UserId = invoice.UserId;
        invoiceData.Status = invoice.Status;
        invoiceData.SeriesId = invoice.SeriesId;
        invoiceData.SeriesName = invoice.SeriesName;
        invoiceData.InvoiceNumber = invoice.InvoiceNumber;

        await Validate(invoiceData);

        InvoiceEntity invoiceEntity = invoiceData.ToEntity();

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

    public async Task<int> MarkOverdue(DateOnly today)
    {
        return await _invoiceRepository.MarkOverdue(today.ToDateTime(TimeOnly.MinValue));
    }

    // Invoice items don't copy name or price: reads join the item (BIL-113). GetVersionsForCustomer
    // only checks that every invoice item's item is the customer's and that its version exists.
    private async Task<CustomerModel> Validate(InvoiceModel invoiceData)
    {
        await _sellerService.GetForUser(invoiceData.SellerId, invoiceData.UserId);
        CustomerModel customer = await _customerService.GetForSeller(invoiceData.CustomerId, invoiceData.SellerId);
        await _itemService.GetVersionsForCustomer(invoiceData.Items!, invoiceData.CustomerId);

        return customer;
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
