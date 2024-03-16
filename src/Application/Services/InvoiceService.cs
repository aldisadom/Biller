using Application.Interfaces;
using Application.Models;
using AutoMapper;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IMapper _mapper;
    private readonly IInvoiceAddressService _invoiceAddressService;
    private readonly IInvoiceItemService _invoiceItemService;
    private readonly IUserService _userService;

    private DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    private DocumentSettings GetSettings() => DocumentSettings.Default;

    public InvoiceService(IMapper mapper, IUserService userService, IInvoiceAddressService invoiceAddressService, IInvoiceItemService invoiceItemService)
    {
        _mapper = mapper;
        _userService = userService;
        _invoiceItemService = invoiceItemService;
        _invoiceAddressService = invoiceAddressService;
    }

    private async Task GenerateInvoiceNumber(InvoiceModel invoice)
    {
        invoice.InvoiceNumber = 50;
    }

    private void GenerateInvoiceFolderPath(UserModel user, InvoiceModel invoice)
    {
        invoice.FolderPath = $"Data/Invoices/{user.Email}";
        if (!Directory.Exists(invoice.FolderPath))
            Directory.CreateDirectory(invoice.FolderPath);
    }

    private void GenerateInvoiceName(InvoiceModel invoice)
    {
        invoice.InvoiceName = $"{invoice.InvoiceNumber}.pdf";
        invoice.FilePath = $"{invoice.FolderPath}/{invoice.InvoiceName}";
    }

    private async Task<InvoiceModel> GetInvoiceDetails(InvoiceDataModel invoiceModel)
    {
        InvoiceModel invoice = new ();
        UserModel user = await _userService.Get(invoiceModel.UserId);
        
        GenerateInvoiceFolderPath(user, invoice);
        await GenerateInvoiceNumber(invoice);

        invoice.IssueDate = DateTime.Now;

        /* fix in nex release */
        invoice.DueDate = DateTime.Now;
        invoice.Comments = string.Empty;

        GenerateInvoiceName(invoice);

        invoice.SellerAddress = await _invoiceAddressService.Get(invoiceModel.SellerAddressId);
        invoice.CustomerAddress = await _invoiceAddressService.Get(invoiceModel.CustomerAddressId);
        invoice.Items = (await _invoiceItemService.Get(invoiceModel.ItemsId)).ToList();
        
        return invoice;
    }

    public async Task GeneratePDF(InvoiceDataModel invoiceModel)
    {
        InvoiceModel invoice = await GetInvoiceDetails(invoiceModel);
        InvoiceDocument document = new (invoice);
        document.GeneratePdf(invoice.FilePath);
    }    
}
