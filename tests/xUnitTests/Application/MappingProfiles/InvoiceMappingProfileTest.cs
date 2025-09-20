using Application.MappingProfiles;
using Application.Models;
using Common.Enums;
using Contracts.Requests.Customer;
using Contracts.Requests.Invoice;
using Contracts.Requests.Seller;
using Domain.Entities;
using FluentAssertions;
using Newtonsoft.Json;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class InvoiceMappingProfileTest
{
    private static readonly UserModel userModel;
    private static readonly SellerModel sellerModel;
    private static readonly CustomerModel customerModel;
    private static readonly List<InvoiceItemModel> invoiceItems;

    static InvoiceMappingProfileTest()
    {
        userModel = new UserModel
        {
            Id = Guid.NewGuid(),
            Name = "UserName",
            LastName = "UserLastName",
            Email = "user@email.com"
        };

        sellerModel = new SellerModel
        {
            Id = Guid.NewGuid(),
            UserId = userModel.Id,
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

        customerModel = new CustomerModel
        {
            Id = Guid.NewGuid(),
            SellerId = sellerModel.Id,
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            InvoiceName = "INV",
            InvoiceNumber = 6
        };

        invoiceItems =
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
    }

    [Fact]
    public void ToEntity_FromModel_ShouldMap()
    {
        // Arrange
        var from = new InvoiceModel
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = customerModel.InvoiceNumber,
            User = userModel,
            Seller = sellerModel,
            Customer = customerModel,
            Items = invoiceItems,
            Comments = "invoice comment",
            Status = InvoiceStatus.Send,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act
        var to = from.ToEntity();

        // Assert
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
            "FilePath",
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
        to.FilePath.Should().Be(from.GenerateFileLocation());
        to.TotalPrice.Should().Be(from.CalculateTotal());
    }

    [Fact]
    public void ToModel_FromEntity_ShouldMap()
    {
        // Arrange
        var from = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = customerModel.InvoiceNumber,
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

        // Act
        var to = from.ToModel();

        // Assert
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
    public void ToModel_FromAddRequest_ShouldMap()
    {
        // Arrange
        var from = new InvoiceAddRequest
        {
            UserId = userModel.Id,
            SellerId = sellerModel.Id,
            CustomerId = customerModel.Id,
            Items = new()
            {
                new InvoiceItemRequest(){ Id = invoiceItems[0].Id, Quantity = invoiceItems[0].Quantity, Comments = invoiceItems[0].Comments },
                new InvoiceItemRequest(){ Id = invoiceItems[1].Id, Quantity = invoiceItems[1].Quantity, Comments = invoiceItems[1].Comments }
            },
            Comments = "invoice comment",
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act
        var to = from.ToModel();

        // Assert
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
    public void ToModel_FromUpdateRequest_ShouldMap()
    {
        // Arrange
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
            InvoiceName = "INV",
            InvoiceNumber = 6
        };

        var itemsUpdate = new List<InvoiceItemUpdateRequest>
        {
            new InvoiceItemUpdateRequest
            {
                Id = Guid.NewGuid(),
                Name = "ItemUpdateName",
                Quantity = 6699,
                Price = 11123.67M
            }
        };

        var from = new InvoiceUpdateRequest
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 963,
            Seller = sellerUpdate,
            Customer = customerUpdate,
            Items = itemsUpdate,
            Comments = "invoice comment",
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act
        var to = from.ToModel();

        // Assert
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
    public void ToResponse_FromModel_ShouldMap()
    {
        // Arrange
        var from = new InvoiceModel
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = customerModel.InvoiceNumber,
            User = userModel,
            Seller = sellerModel,
            Customer = customerModel,
            Items = invoiceItems,
            Comments = "invoice comment",
            Status = InvoiceStatus.Send,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act
        var to = from.ToResponse();

        // Assert
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
    public void ToInvoiceItemResponse_FromInvoiceItemModel_ShouldMap()
    {
        // Arrange
        var from = new InvoiceItemModel
        {
            Id = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M,
            Comments = "Item comment"
        };


        // Act
        var to = from.ToInvoiceItemResponse();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToInvoiceItemModel_FromInvoiceItemRequest_ShouldMap()
    {
        // Arrange
        var from = new InvoiceItemRequest
        {
            Id = Guid.NewGuid(),
            Quantity = 66,
            Comments = "Item comment"
        };

        // Act
        var to = from.ToInvoiceItemModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToInvoiceItemModel_FromInvoiceItemUpdateRequest_ShouldMap()
    {
        // Arrange
        var from = new InvoiceItemUpdateRequest
        {
            Id = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M,
            Comments = "Item comment"
        };

        // Act
        var to = from.ToInvoiceItemModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }
}
