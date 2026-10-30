using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using Domain.Entities;
using FluentAssertions;
using Newtonsoft.Json;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class InvoiceMappingProfileTest
{
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

    private static InvoiceModel NewInvoice() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        SeriesId = Guid.NewGuid(),
        SeriesName = "INV",
        InvoiceNumber = 6,
        Items = NewItems(),
        Comments = "invoice comment",
        Status = InvoiceStatus.Send,
        CreatedDate = DateOnly.FromDateTime(DateTime.Now),
        DueDate = DateOnly.FromDateTime(DateTime.Now)
    };

    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
    {
        //Arrange
        InvoiceModel from = NewInvoice();

        //Act
        var to = from.ToEntity();

        //Assert
        HashSet<string> ignoreProperties = [
            "ItemsData",
            "CreatedDate",
            "DueDate",
            "TotalPrice"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.ItemsData.Should().Be(JsonConvert.SerializeObject(from.Items));
        to.CreatedDate.Should().Be(from.CreatedDate.ToDateTime(TimeOnly.MinValue));
        to.DueDate.Should().Be(from.DueDate.ToDateTime(TimeOnly.MinValue));
        to.TotalPrice.Should().Be(51.5m);
    }

    [Fact]
    public void ToFull_GivenModelAndUserSellerCustomer_MapsAllProperties()
    {
        //Arrange
        InvoiceModel from = NewInvoice();
        UserModel user = new() { Id = from.UserId };
        SellerModel seller = new() { Id = from.SellerId };
        CustomerModel customer = new() { Id = from.CustomerId };

        //Act
        var to = from.ToFull(user, seller, customer);

        //Assert
        HashSet<string> ignoreProperties = [
            "User",
            "Seller",
            "Customer"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.User.Should().BeSameAs(user);
        to.Seller.Should().BeSameAs(seller);
        to.Customer.Should().BeSameAs(customer);
    }

    [Fact]
    public void ToModel_GivenEntity_MapsAllProperties()
    {
        //Arrange
        List<InvoiceItemModel> invoiceItems = NewItems();

        var from = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV",
            InvoiceNumber = 6,
            UserId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
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
            "Items",
            "CreatedDate",
            "DueDate"
        ];

        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.Items.Should().BeEquivalentTo(invoiceItems);
        to.CreatedDate.Should().Be(DateOnly.FromDateTime(from.CreatedDate));
        to.DueDate.Should().Be(DateOnly.FromDateTime(from.DueDate));
    }

    [Fact]
    public void ToModel_GivenAddRequest_MapsAllProperties()
    {
        //Arrange
        var from = new InvoiceAddRequest
        {
            UserId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = NewItems()
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
            "Items"
        ];

        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom, ignoreProperties);

        to.Items.Should().BeEquivalentTo(from.Items.Select(i => i.ToInvoiceItemModel()));
    }

    [Fact]
    public void ToModel_GivenUpdateRequest_MapsAllProperties()
    {
        //Arrange
        var from = new InvoiceUpdateRequest
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new() { Id = Guid.NewGuid(), Quantity = 6699, Comments = "Item comment" }
            ],
            Comments = "invoice comment",
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateOnly.FromDateTime(DateTime.Now)
        };

        //Act
        var to = from.ToModel();

        //Assert
        HashSet<string> ignoreProperties = [
            "Items"
        ];

        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom, ignoreProperties);

        to.Items.Should().BeEquivalentTo(from.Items.Select(i => i.ToInvoiceItemModel()));
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
}
