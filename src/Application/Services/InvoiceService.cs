using Application.Interfaces;
using Application.Models;
using AutoMapper;
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

    private DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    private DocumentSettings GetSettings() => DocumentSettings.Default;

    public InvoiceService(IMapper mapper, IUserService userService, ICustomerService CustomerService, IItemService itemService, ISellerService sellerService)
    {
        _mapper = mapper;
        _userService = userService;
        _itemService = itemService;
        _customerService = CustomerService;
        _sellerService = sellerService;
    }

    private void GenerateInvoiceFolderPath(UserModel user, InvoiceModel invoice)
    {
        invoice.FolderPath = $"Data/Invoices/{user.Id}/{invoice.Seller.Id}/{invoice.Customer.Id}";
        if (!Directory.Exists(invoice.FolderPath))
            Directory.CreateDirectory(invoice.FolderPath);
    }

    private void GenerateInvoiceName(InvoiceModel invoice)
    {
        invoice.Name = invoice.Customer.InvoiceName+"-";

        if (invoice.Customer.InvoiceNumber < 10)
            invoice.Name += "00000";
        else if (invoice.Customer.InvoiceNumber < 100)
            invoice.Name += "0000";
        else if (invoice.Customer.InvoiceNumber < 1000)
            invoice.Name += "000";
        else if (invoice.Customer.InvoiceNumber < 10000)
            invoice.Name += "00";
        else if (invoice.Customer.InvoiceNumber < 100000)
            invoice.Name += "0";

        invoice.Name += $"{invoice.Customer.InvoiceNumber}.pdf";
        invoice.FilePath = $"{invoice.FolderPath}/{invoice.Name}";
    }

    private async Task<InvoiceModel> GetInvoiceDetails(InvoiceDataModel invoiceModel)
    {
        InvoiceModel invoice = new();
        UserModel user = await _userService.Get(invoiceModel.UserId);

        await _customerService.UpdateInvoiceNumber(invoiceModel.CustomerAddressId);

        invoice.Seller = await _sellerService.Get(invoiceModel.SellerAddressId);
        invoice.Customer = await _customerService.Get(invoiceModel.CustomerAddressId);
        invoice.Items = (await _itemService.Get(invoiceModel.ItemsId)).ToList();

        invoice.IssueDate = DateTime.Now;
        /* fix in nex release */
        invoice.DueDate = DateTime.Now;
        invoice.Comments = string.Empty;

        GenerateInvoiceFolderPath(user, invoice);
        GenerateInvoiceName(invoice);

        return invoice;
    }

    public async Task GeneratePDF(InvoiceDataModel invoiceModel)
    {
        InvoiceModel invoice = await GetInvoiceDetails(invoiceModel);
        InvoiceDocument document = new(invoice);
        document.GeneratePdf(invoice.FilePath);
    }
}
