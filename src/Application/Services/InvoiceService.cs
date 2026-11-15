using Application.Helpers;
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
    private readonly IDocumentTextChecker _documentTextChecker;

    public InvoiceService(IUserService userService, ICustomerService CustomerService,
                        IItemService itemService, ISellerService sellerService, IInvoiceRepository invoiceRepository, IInvoiceDocumentFactory invoiceDocumentFactory,
                        IDocumentTextChecker documentTextChecker)
    {
        _documentTextChecker = documentTextChecker;
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

    public async Task<(IEnumerable<InvoiceModelFull> Items, int TotalCount, int Page)> Get(InvoiceGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount, returnedPage) = await _invoiceRepository.Get(
            query?.UserId, query?.SellerId, query?.CustomerId, page, (int)pageSize);

        List<InvoiceModel> invoices = entities.Select(i => i.ToModel()).ToList();

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

        var (customer, items) = await Validate(invoiceData);
        invoiceData.SeriesId = customer.SeriesId;
        invoiceData.Status = InvoiceStatus.Created;
        MapItemToInvoiceItem(invoiceData.Items!, items);

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

        var (_, items) = await Validate(invoiceData);
        MapItemToInvoiceItem(invoiceData.Items!, items);

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
        CheckDocumentTexts(invoiceData);

        return (_invoiceDocumentFactory.GeneratePdf(documentType, languageCode, invoiceData), invoiceData.GenerateFileLocation());
    }

    // Text saved before the validators checked it can still have a character the PDF font can't
    // draw. The message names every such field and its characters, not the text: it is logged.
    private void CheckDocumentTexts(InvoiceModelFull invoice)
    {
        List<string> errors = [];

        void Check(string field, string? text, string howToFix)
        {
            IReadOnlyList<string> missing = _documentTextChecker.MissingCharacters(text);
            if (missing.Count != 0)
                errors.Add($"{field} has characters the invoice PDF can't show: {DocumentTextFormat.Describe(missing)}. {howToFix}");
        }

        const string editSeller = "Edit the seller";
        const string editCustomer = "Edit the customer";
        const string editUser = "Edit the user";
        const string editInvoice = "Edit the invoice";

        Check("Series name", invoice.SeriesName, "A series with invoices can't be renamed through the API: its name has to be fixed in the database");
        Check("User name", invoice.User.Name, editUser);
        Check("User last name", invoice.User.LastName, editUser);

        Check("Seller company name", invoice.Seller.CompanyName, editSeller);
        Check("Seller company number", invoice.Seller.CompanyNumber, editSeller);
        Check("Seller street", invoice.Seller.Street, editSeller);
        Check("Seller city", invoice.Seller.City, editSeller);
        Check("Seller phone", invoice.Seller.Phone, editSeller);
        Check("Seller email", invoice.Seller.Email, editSeller);
        Check("Seller bank name", invoice.Seller.BankName, editSeller);
        Check("Seller bank number", invoice.Seller.BankNumber, editSeller);

        Check("Customer company name", invoice.Customer.CompanyName, editCustomer);
        Check("Customer company number", invoice.Customer.CompanyNumber, editCustomer);
        Check("Customer street", invoice.Customer.Street, editCustomer);
        Check("Customer city", invoice.Customer.City, editCustomer);
        Check("Customer phone", invoice.Customer.Phone, editCustomer);
        Check("Customer email", invoice.Customer.Email, editCustomer);

        List<InvoiceItemModel> items = invoice.Items ?? [];
        for (int i = 0; i < items.Count; i++)
        {
            Check($"Item name on line {i + 1}", items[i].Name, "Edit the item, then save the invoice again");
            Check($"Comment on line {i + 1}", items[i].Comments, editInvoice);
        }

        Check("Invoice comment", invoice.Comments, editInvoice);

        if (errors.Count != 0)
            throw new DocumentTextException(string.Join("\n", errors));
    }
}
