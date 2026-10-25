using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Customer;
using BillerContracts.Requests.Invoice;
using BillerContracts.Requests.Seller;
using Domain.Entities;
using FluentAssertions;
using Newtonsoft.Json;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class InvoiceMappingProfileTest
{
    private static UserModel NewUser() => new UserModel
    {
        Id = Guid.NewGuid(),
        Name = "UserName",
        LastName = "UserLastName",
        Email = "user@email.com"
    };

    private static SellerModel NewSeller(Guid userId) => new SellerModel
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CompanyName = "CompanyName",
        CompanyNumber = "123456",
        Street = "Main St",
        City = "Vilnius",
        State = "LT",
        Email = "test@email.com",
        Phone = "+37060000000",
        BankName = "Bank",
        BankNumber = "IBan"
    };

    private static CustomerModel NewCustomer(Guid sellerId) => new CustomerModel
    {
        Id = Guid.NewGuid(),
        SellerId = sellerId,
        CompanyName = "CompanyName",
        CompanyNumber = "123456",
        Street = "Main St",
        City = "Vilnius",
        State = "LT",
        Email = "test@email.com",
        Phone = "+37060000000",
        SeriesId = Guid.NewGuid(),
        SeriesName = "INV"
    };

    private static List<InvoiceItemModel> NewItems() =>
    [
        new InvoiceItemModel
        {
            Id = Guid.NewGuid(),
            Name = "Item 1",
            Quantity = 1,
            Price = 10.5m,
            Comments = "Item 1 comment",
        },
        new InvoiceItemModel
        {
            Id = Guid.NewGuid(),
            Name = "Item 2",
            Quantity = 2,
            Price = 20.5m,
            Comments = "Item 2 comment",
        }
    ];

    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
    {
        //Arrange
        UserModel userModel = NewUser();
        SellerModel sellerModel = NewSeller(userModel.Id);
        CustomerModel customerModel = NewCustomer(sellerModel.Id);
        List<InvoiceItemModel> invoiceItems = NewItems();

        var from = new InvoiceModel
        {
            Id = Guid.NewGuid(),
            SeriesId = customerModel.SeriesId,
            SeriesName = customerModel.SeriesName,
            InvoiceNumber = 6,
            User = userModel,
            Seller = sellerModel,
            Customer = customerModel,
            Items = invoiceItems,
            Comments = "invoice comment",
            Status = InvoiceStatus.Send,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        //Act
        var to = from.ToEntity();

        //Assert
        HashSet<string> ignoreProperties = [
            "UserData",
            "SellerData",
            "CustomerData",
            "ItemsData",

            "UserId",
            "SellerId",
            "CustomerId",

            "CreatedDate",
            "DueDate",
            "TotalPrice"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.UserData.Should().Be(JsonConvert.SerializeObject(userModel));
        to.SellerData.Should().Be(JsonConvert.SerializeObject(sellerModel));
        to.CustomerData.Should().Be(JsonConvert.SerializeObject(customerModel));
        to.ItemsData.Should().Be(JsonConvert.SerializeObject(invoiceItems));

        to.UserId.Should().Be(userModel.Id);
        to.SellerId.Should().Be(sellerModel.Id);
        to.CustomerId.Should().Be(customerModel.Id);

        to.CreatedDate.Should().Be(from.CreatedDate.ToDateTime(TimeOnly.MinValue));
        to.DueDate.Should().Be(from.DueDate.ToDateTime(TimeOnly.MinValue));
        to.TotalPrice.Should().Be(from.CalculateTotal());
    }

    [Fact]
    public void ToModel_GivenEntity_MapsAllProperties()
    {
        //Arrange
        UserModel userModel = NewUser();
        SellerModel sellerModel = NewSeller(userModel.Id);
        CustomerModel customerModel = NewCustomer(sellerModel.Id);
        List<InvoiceItemModel> invoiceItems = NewItems();

        var from = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            SeriesId = customerModel.SeriesId,
            SeriesName = customerModel.SeriesName,
            InvoiceNumber = 6,
            UserId = userModel.Id,
            SellerId = sellerModel.Id,
            CustomerId = customerModel.Id,
            UserData = JsonConvert.SerializeObject(userModel),
            SellerData = JsonConvert.SerializeObject(sellerModel),
            CustomerData = JsonConvert.SerializeObject(customerModel),
            ItemsData = JsonConvert.SerializeObject(invoiceItems),
            Comments = "invoice comment",
            Status = InvoiceStatus.Send,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue),
            DueDate = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue)
        };

        //Act
        var to = from.ToModel();

        //Assert
        HashSet<string> ignoreProperties = [
            "User",
            "Seller",
            "Customer",
            "Items",

            "CreatedDate",
            "DueDate"
        ];

        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.User.Should().BeEquivalentTo(userModel);
        to.Seller.Should().BeEquivalentTo(sellerModel);
        to.Customer.Should().BeEquivalentTo(customerModel);
        to.Items.Should().BeEquivalentTo(invoiceItems);

        to.CreatedDate.Should().Be(DateOnly.FromDateTime(from.CreatedDate));
        to.DueDate.Should().Be(DateOnly.FromDateTime(from.DueDate));
    }

    [Fact]
    public void ToModel_GivenEntityWithCustomerDataFromBeforeSeries_TakesSeriesFromInvoice()
    {
        //Arrange
        var from = new InvoiceEntity
        {
            SeriesId = Guid.NewGuid(),
            SeriesName = "NEW",
            UserData = "{}",
            SellerData = "{}",
            CustomerData = "{\"CompanyName\":\"CompanyName\",\"InvoiceName\":\"OLD\",\"InvoiceNumber\":6}",
            ItemsData = "[]"
        };

        //Act
        var to = from.ToModel();

        //Assert
        to.Customer!.CompanyName.Should().Be("CompanyName");
        to.Customer.SeriesId.Should().Be(from.SeriesId);
        to.Customer.SeriesName.Should().Be("NEW");
    }

    [Fact]
    public void ToModel_GivenAddRequest_MapsAllProperties()
    {
        //Arrange
        UserModel userModel = NewUser();
        SellerModel sellerModel = NewSeller(userModel.Id);
        CustomerModel customerModel = NewCustomer(sellerModel.Id);
        List<InvoiceItemModel> invoiceItems = NewItems();

        var from = new InvoiceAddRequest
        {
            UserId = userModel.Id,
            SellerId = sellerModel.Id,
            CustomerId = customerModel.Id,
            Items = invoiceItems
                .Select(item => new InvoiceItemRequest() { Id = item.Id, Quantity = item.Quantity, Comments = item.Comments })
                .ToList(),
            Comments = "invoice comment",
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        //Act
        var to = from.ToModel();

        //Assert
        HashSet<string> ignoreProperties = [
            "UserId",
            "SellerId",
            "CustomerId",
            "Items"
        ];

        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom, ignoreProperties);

        to.User.Should().NotBeNull();
        to.Seller.Should().NotBeNull();
        to.Customer.Should().NotBeNull();
        to.Items.Should().NotBeNull();

        to.User!.Id.Should().Be(from.UserId);
        to.Seller!.Id.Should().Be(from.SellerId);
        to.Customer!.Id.Should().Be(from.CustomerId);

        to.Items!.Count.Should().Be(from.Items.Count);
        to.Items.Should().BeEquivalentTo(from.Items.Select(i => i.ToInvoiceItemModel()));
    }

    [Fact]
    public void ToModel_GivenUpdateRequest_MapsAllProperties()
    {
        //Arrange
        var sellerUpdate = new SellerUpdateRequest
        {
            Id = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
        };
        var customerUpdate = new CustomerUpdateRequest
        {
            Id = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid()
        };

        var itemsUpdate = new List<InvoiceItemUpdateRequest>
        {
            new() {
                Id = Guid.NewGuid(),
                Name = "ItemUpdateName",
                Quantity = 6699,
                Price = 11123.67M
            }
        };

        var from = new InvoiceUpdateRequest
        {
            Id = Guid.NewGuid(),
            Seller = sellerUpdate,
            Customer = customerUpdate,
            Items = itemsUpdate,
            Comments = "invoice comment",
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        //Act
        var to = from.ToModel();

        //Assert
        HashSet<string> ignoreProperties = [
            "Seller",
            "Customer",
            "Items"
        ];

        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom, ignoreProperties);

        to.Seller.Should().NotBeNull();
        to.Customer.Should().NotBeNull();
        to.Items.Should().NotBeNull();

        to.Seller.Should().Be(from.Seller.ToModel());
        to.Customer.Should().Be(from.Customer.ToModel());

        to.Items!.Count.Should().Be(from.Items.Count);
        to.Items.Should().BeEquivalentTo(from.Items.Select(i => i.ToInvoiceItemModel()));
    }

    [Fact]
    public void ToResponse_GivenModel_MapsAllProperties()
    {
        //Arrange
        UserModel userModel = NewUser();
        SellerModel sellerModel = NewSeller(userModel.Id);
        CustomerModel customerModel = NewCustomer(sellerModel.Id);
        List<InvoiceItemModel> invoiceItems = NewItems();

        var from = new InvoiceModel
        {
            Id = Guid.NewGuid(),
            SeriesId = customerModel.SeriesId,
            SeriesName = customerModel.SeriesName,
            InvoiceNumber = 6,
            User = userModel,
            Seller = sellerModel,
            Customer = customerModel,
            Items = invoiceItems,
            Comments = "invoice comment",
            Status = InvoiceStatus.Send,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        //Act
        var to = from.ToResponse();

        //Assert
        HashSet<string> ignoreProperties = [
            "UserId",
            "Seller",
            "Customer",
            "Items",

            "TotalPrice"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);
        to.UserId.Should().Be(userModel.Id);
        to.Seller.Should().Be(sellerModel.ToResponse());
        to.Customer.Should().Be(customerModel.ToResponse());
        to.Items.Should().BeEquivalentTo(invoiceItems.Select(i => i.ToInvoiceItemResponse()));

        to.TotalPrice.Should().Be(from.CalculateTotal());
    }

    [Fact]
    public void ToInvoiceItemResponse_GivenInvoiceItemModel_MapsAllProperties()
    {
        //Arrange
        var from = new InvoiceItemModel
        {
            Id = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M,
            Comments = "Item comment"
        };


        //Act
        var to = from.ToInvoiceItemResponse();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToInvoiceItemModel_GivenInvoiceItemRequest_MapsAllProperties()
    {
        //Arrange
        var from = new InvoiceItemRequest
        {
            Id = Guid.NewGuid(),
            Quantity = 66,
            Comments = "Item comment"
        };

        //Act
        var to = from.ToInvoiceItemModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToInvoiceItemModel_GivenInvoiceItemUpdateRequest_MapsAllProperties()
    {
        //Arrange
        var from = new InvoiceItemUpdateRequest
        {
            Id = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M,
            Comments = "Item comment"
        };

        //Act
        var to = from.ToInvoiceItemModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }
}
