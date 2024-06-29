using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Newtonsoft.Json;
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

    private static void GenerateInvoiceFolderPath(InvoiceDataModel invoiceData)
    {
        string folderPath = invoiceData.GenerateFolderLocation();
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    private static void GenerateInvoiceName(InvoiceDataModel invoiceData)
    {
        invoiceData.Customer!.InvoiceNumber++;
        if (invoiceData.Customer.InvoiceNumber < 10)
            invoiceData.Number = "00000";
        else if (invoiceData.Customer.InvoiceNumber < 100)
            invoiceData.Number = "0000";
        else if (invoiceData.Customer.InvoiceNumber < 1000)
            invoiceData.Number = "000";
        else if (invoiceData.Customer.InvoiceNumber < 10000)
            invoiceData.Number = "00";
        else if (invoiceData.Customer.InvoiceNumber < 100000)
            invoiceData.Number = "0";

        invoiceData.Number += invoiceData.Customer.InvoiceNumber.ToString();
    }

    private async Task GetInvoiceDetails(InvoiceDataModel invoiceData)
    {
        invoiceData.User = await _userService.Get(invoiceData.User!.Id);
        invoiceData.Seller = await _sellerService.Get(invoiceData.Seller!.Id);
        invoiceData.Customer = await _customerService.Get(invoiceData.Customer!.Id);
        List<ItemModel> items = (await _itemService.Get(invoiceData.Items!.Select(x => x.Id).ToList())).ToList();

        MapItemToInvoiceItem(invoiceData.Items!, items);

        GenerateInvoiceFolderPath(invoiceData);
        GenerateInvoiceName(invoiceData);
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

    public ItemModel MapInvoiceItem(InvoiceItemModel invoiceItem)
    {
        return new ItemModel()
        {
            Id = invoiceItem.Id,
            Price = invoiceItem.Price,
            Name = invoiceItem.Name
        };
    }

    public InvoiceDataEntity MapInvoiceData(InvoiceDataModel invoiceData)
    {
        return  new InvoiceDataEntity()
        {
            Id = invoiceData.Id,
            SellerId = invoiceData.Seller!.Id,
            CustomerId = invoiceData.Customer!.Id,
            UserId = invoiceData.User!.Id,

            FilePath = invoiceData.GenerateFileLocation(),
            Number = invoiceData.Number,
            UserData = JsonConvert.SerializeObject(invoiceData.User),
            CreatedDate = invoiceData.CreatedDate,
            DueDate = invoiceData.DueDate,
            SellerData = JsonConvert.SerializeObject(invoiceData.Seller),
            CustomerData = JsonConvert.SerializeObject(invoiceData.Customer),
            Comments = invoiceData.Comments,
            TotalPrice = invoiceData.CalculateTotal(),
            ItemsData = JsonConvert.SerializeObject(invoiceData.Items)
        };
    }

    public InvoiceDataModel MapInvoiceData(InvoiceDataEntity invoiceData)
    {
        return new InvoiceDataModel()
        {
            Id = invoiceData.Id,
            Number = invoiceData.Number,
            User = JsonConvert.DeserializeObject<UserModel>(invoiceData.UserData),
            CreatedDate = invoiceData.CreatedDate,
            DueDate = invoiceData.DueDate,
            Seller = JsonConvert.DeserializeObject<SellerModel>(invoiceData.SellerData),
            Customer = JsonConvert.DeserializeObject<CustomerModel>(invoiceData.CustomerData),
            Comments = invoiceData.Comments,
            Items = JsonConvert.DeserializeObject<List<InvoiceItemModel>>(invoiceData.ItemsData)
        };
    }

    public async Task<InvoiceDataModel> Get(Guid id)
    {
        InvoiceDataEntity invoiceDataEntity = await _invoiceRepository.Get(id)
            ?? throw new NotFoundException($"Invoice data:{id} not found");

        return MapInvoiceData(invoiceDataEntity);
    }

    public async Task<IEnumerable<InvoiceDataModel>> Get(InvoiceDataGetRequest query)
    {
        IEnumerable<InvoiceDataEntity> invoiceDataEntities;
                
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

        return invoiceDataEntities.Select(i=>MapInvoiceData(i));
    }

    public async Task<Guid> Add(InvoiceDataModel invoiceData)
    {
        await GetInvoiceDetails(invoiceData);

        InvoiceDataEntity invoiceDataEntity = MapInvoiceData(invoiceData);

        Guid id = await _invoiceRepository.Add(invoiceDataEntity);
        await _customerService.UpdateInvoiceNumber(invoiceData!.Customer.Id);

        return id;
    }

    public async Task Update(InvoiceDataModel invoiceData)
    {
        await Get(invoiceData.Id);

        InvoiceDataEntity invoiceEntity = MapInvoiceData(invoiceData);

        await _invoiceRepository.Update(invoiceEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _invoiceRepository.Delete(id);
    }

    public async Task GeneratePDF(Guid id)
    {
        InvoiceDataModel invoiceData = await Get(id);
        InvoiceDocument document = new(invoiceData);
        document.GeneratePdf(invoiceData.GenerateFileLocation());

        InvoiceDocumentADA documentADA = new(invoiceData);
        documentADA.GeneratePdf(invoiceData.GenerateFileLocation() + "a");
    }
}
