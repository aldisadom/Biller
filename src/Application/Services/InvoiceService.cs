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
    private string _fileName = "Invoice.pdf";

    private DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    private DocumentSettings GetSettings() => DocumentSettings.Default;

    public InvoiceService(IMapper mapper, IUserService userService, IInvoiceAddressService invoiceAddressService, IInvoiceItemService invoiceItemService)
    {
        _mapper = mapper;
        _userService = userService;
        _invoiceItemService = invoiceItemService;
        _invoiceAddressService = invoiceAddressService;
    }

    private async Task<int> GenerateInvoiceNumber(string invoicePath)
    {
        return 50;
    }

    private string GenerateInvoicePath(UserModel user, InvoiceModel invoice)
    {
        string filePath = $"Data/Invoices/{user.Email}";
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        return filePath;
    }

    private void GenerateInvoiceName(string invoicePath, int invoiceNumber, UserModel user)
    {
        _fileName = $"{invoicePath}/{invoiceNumber}.pdf";
    }

    private async Task<InvoiceModel> GetInvoiceDetails(InvoiceDataModel invoiceModel)
    {
        InvoiceModel invoice = new ();
        UserModel user = await _userService.Get(invoiceModel.UserId);
        string invoicePath = GenerateInvoicePath(user);

        invoice.InvoiceNumber = await GenerateInvoiceNumber(invoicePath);
        invoice.IssueDate = DateTime.Now;
        invoice.DueDate = DateTime.Now;
        invoice.Comments = string.Empty;
        await GenerateInvoiceName(invoicePath, user);

        invoice.SellerAddress = await _invoiceAddressService.Get(invoiceModel.SellerAddressId);
        invoice.CustomerAddress = await _invoiceAddressService.Get(invoiceModel.CustomerAddressId);
        invoice.Items = (await _invoiceItemService.Get(invoiceModel.ItemsId)).ToList();
        
        return invoice;
    }

    public async Task GeneratePDF(InvoiceDataModel invoiceModel)
    {
        InvoiceDocument document = new (await GetInvoiceDetails(invoiceModel));
        document.GeneratePdf(_fileName);
    }    
}
