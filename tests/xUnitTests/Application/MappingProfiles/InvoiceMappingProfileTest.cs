using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using Domain.Entities;
using AwesomeAssertions;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class InvoiceMappingProfileTest
{
    private static List<InvoiceItemModel> NewItems() =>
    [
        new InvoiceItemModel
        {
            Id = Guid.NewGuid(),
            Version = 2,
            Name = "Item 1",
            Quantity = 1,
            Price = 10.5m,
            Comments = "Item 1 comment",
        },
        new InvoiceItemModel
        {
            Id = Guid.NewGuid(),
            Version = 3,
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
            "Items",
            "CreatedDate",
            "DueDate"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        // An invoice item keeps its line number, item, version, quantity and comments; name and price aren't stored
        to.Items.Should().BeEquivalentTo(new List<InvoiceItemEntity>
        {
            new() { InvoiceId = from.Id, LineNo = 1, ItemId = from.Items![0].Id, Version = 2, Quantity = 1, Comments = "Item 1 comment" },
            new() { InvoiceId = from.Id, LineNo = 2, ItemId = from.Items[1].Id, Version = 3, Quantity = 2, Comments = "Item 2 comment" }
        }, o => o.WithStrictOrdering());
        to.CreatedDate.Should().Be(from.CreatedDate.ToDateTime(TimeOnly.MinValue));
        to.DueDate.Should().Be(from.DueDate.ToDateTime(TimeOnly.MinValue));
    }

    [Fact]
    public void ToEntity_GivenModelWithoutItems_HasNoItems()
    {
        //Arrange
        InvoiceModel from = new() { Items = [] };

        //Act
        var to = from.ToEntity();

        //Assert
        to.Items.Should().BeEmpty();
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
        Guid firstItemId = Guid.NewGuid();
        Guid secondItemId = Guid.NewGuid();

        var from = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV",
            InvoiceNumber = 6,
            UserId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            // Out of line order, to show the items come back in line order
            Items =
            [
                new() { LineNo = 2, ItemId = secondItemId, Version = 3, Name = "Item 2", Price = 20.5m, Quantity = 2, Comments = "Item 2 comment" },
                new() { LineNo = 1, ItemId = firstItemId, Version = 2, Name = "Item 1", Price = 10.5m, Quantity = 1, Comments = "Item 1 comment" }
            ],
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

        to.Items.Should().BeEquivalentTo(new List<InvoiceItemModel>
        {
            new() { Id = firstItemId, Version = 2, Name = "Item 1", Price = 10.5m, Quantity = 1, Comments = "Item 1 comment" },
            new() { Id = secondItemId, Version = 3, Name = "Item 2", Price = 20.5m, Quantity = 2, Comments = "Item 2 comment" }
        }, o => o.WithStrictOrdering());
        to.CreatedDate.Should().Be(DateOnly.FromDateTime(from.CreatedDate));
        to.DueDate.Should().Be(DateOnly.FromDateTime(from.DueDate));
    }

    [Fact]
    public void ToModel_GivenEntityWithoutItems_HasNoItems()
    {
        //Arrange
        InvoiceEntity from = new() { Items = [] };

        //Act
        var to = from.ToModel();

        //Assert
        to.Items.Should().BeEmpty();
    }

    [Fact]
    public void ToInvoiceItemModel_GivenInvoiceItemEntity_MapsAllProperties()
    {
        //Arrange
        InvoiceItemEntity from = new()
        {
            InvoiceId = Guid.NewGuid(),
            LineNo = 3,
            ItemId = Guid.NewGuid(),
            Version = 4,
            Name = "Iron throne",
            Price = 10.5m,
            Quantity = 2,
            Comments = "Winterfell"
        };

        //Act
        var to = from.ToInvoiceItemModel();

        //Assert
        // The model's Id is the entity's ItemId
        HashSet<string> ignoreProperties = ["Id"];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.Id.Should().Be(from.ItemId);
    }

    [Fact]
    public void ToInvoiceItemEntity_GivenInvoiceItemModel_MapsItemVersionQuantityAndCommentsOnly()
    {
        //Arrange
        InvoiceItemModel from = new()
        {
            Id = Guid.NewGuid(),
            Version = 4,
            Name = "Iron throne",
            Price = 10.5m,
            Quantity = 2,
            Comments = "Winterfell"
        };
        Guid invoiceId = Guid.NewGuid();

        //Act
        var to = from.ToInvoiceItemEntity(invoiceId, 3);

        //Assert
        to.Should().BeEquivalentTo(new InvoiceItemEntity
        {
            InvoiceId = invoiceId,
            LineNo = 3,
            ItemId = from.Id,
            Version = 4,
            Quantity = 2,
            Comments = "Winterfell"
        });
    }

    [Fact]
    public void ToInvoiceItemEntity_GivenInvoiceItemModelWithoutVersion_KeepsVersionEmpty()
    {
        //Arrange
        InvoiceItemModel from = new() { Id = Guid.NewGuid() };

        //Act
        var to = from.ToInvoiceItemEntity(Guid.NewGuid(), 1);

        //Assert
        // InvoiceRepository.Add gives an empty version the item's latest; a PUT can't send one empty
        to.Version.Should().BeNull();
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
            Comments = "Item comment",
            Version = 3
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
            Comments = "Item comment",
            Version = 3
        };

        //Act
        var to = from.ToInvoiceItemModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }
}
