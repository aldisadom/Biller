using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IMapper _mapper;
    private readonly ICustomerService _customerService;
    private readonly ISellerService _sellerService;
    private readonly IItemService _itemService;
    private readonly IUserService _userService;
    private readonly IInvoiceRepository _invoiceRepository;

    private DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    private DocumentSettings GetSettings() => DocumentSettings.Default;

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

    private void GenerateInvoiceFolderPath(InvoiceDataModel invoice)
    {
        invoice.FolderPath = $"Data/Invoices/{invoice.User!.Id}/{invoice.Seller!.Id}/{invoice.Customer!.Id}";
        if (!Directory.Exists(invoice.FolderPath))
            Directory.CreateDirectory(invoice.FolderPath);
    }

    private void GenerateInvoiceName(InvoiceDataModel invoice)
    {
        invoice.Name = invoice.Customer!.InvoiceName;

        invoice.Customer.InvoiceNumber++;
        if (invoice.Customer.InvoiceNumber < 10)
            invoice.Number += "00000";
        else if (invoice.Customer.InvoiceNumber < 100)
            invoice.Number += "0000";
        else if (invoice.Customer.InvoiceNumber < 1000)
            invoice.Number += "000";
        else if (invoice.Customer.InvoiceNumber < 10000)
            invoice.Number += "00";
        else if (invoice.Customer.InvoiceNumber < 100000)
            invoice.Number += "0";

        invoice.Number += invoice.Customer.InvoiceNumber.ToString();
        invoice.FilePath = $"{invoice.FolderPath}/{invoice.Name}-{invoice.Number}.pdf";
    }

    private async Task GetInvoiceDetails(InvoiceDataModel invoiceModel)
    {
        invoiceModel.User = await _userService.Get(invoiceModel.UserId);
        invoiceModel.Seller = await _sellerService.Get(invoiceModel.SellerId);
        invoiceModel.Customer = await _customerService.Get(invoiceModel.CustomerId);
        List<ItemModel> itemsModels = (await _itemService.Get(invoiceModel.Items!.Select(x => x.Id).ToList())).ToList();

        invoiceModel.TotalPrice = 0.0m;

        foreach (InvoiceItemModel invoiceItem in invoiceModel.Items!)
        {
            foreach (ItemModel itemModel in itemsModels)
            {
                if (itemModel.Id == invoiceItem.Id)
                {
                    invoiceItem.Price = itemModel.Price;
                    invoiceItem.Name = itemModel.Name;
                    invoiceItem.TotalPrice = invoiceItem.Price * invoiceItem.Quantity;

                    invoiceModel.TotalPrice += invoiceItem.TotalPrice;
                    break;
                }
            }
        }

        GenerateInvoiceFolderPath(invoiceModel);
        GenerateInvoiceName(invoiceModel);
    }

    public async Task<Guid> Add(InvoiceDataModel invoiceModel)
    {
        await GetInvoiceDetails(invoiceModel);

        InvoiceDataEntity invoice = new()
        {
            SellerId = invoiceModel.SellerId,
            CustomerId = invoiceModel.CustomerId,
            UserId = invoiceModel.UserId,

            FilePath = invoiceModel.FilePath,
            Number = invoiceModel.Number,
            UserData = JsonSerializer.Serialize(invoiceModel.User),
            CreatedDate = invoiceModel.CreatedDate,
            DueDate = invoiceModel.DueDate,
            SellerData = JsonSerializer.Serialize(invoiceModel.Seller),
            CustomerData = JsonSerializer.Serialize(invoiceModel.Customer),
            Comments = invoiceModel.Comments,
            TotalPrice = invoiceModel.TotalPrice,
            ItemsData = JsonSerializer.Serialize(invoiceModel.Items)
        };

        Guid id = await _invoiceRepository.Add(invoice);
        await _customerService.UpdateInvoiceNumber(invoiceModel.CustomerId);

        GeneratePdf(invoiceModel);

        return id;
    }

    private void GeneratePdf(InvoiceDataModel invoiceModel)
    {
        InvoiceDocument document = new(invoiceModel);
        document.GeneratePdf(invoiceModel.FilePath);

        InvoiceDocumentADA documentADA = new(invoiceModel);
        documentADA.GeneratePdf(invoiceModel.FilePath+"a");
    }
}
