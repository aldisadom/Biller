using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using FluentAssertions;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class InvoiceFullMappingProfileTest
{
    [Fact]
    public void ToResponse_GivenModel_MapsAllProperties()
    {
        //Arrange
        UserModel userModel = new() { Id = Guid.NewGuid() };
        SellerModel sellerModel = new() { Id = Guid.NewGuid(), CompanyName = "Lannister gold" };
        CustomerModel customerModel = new()
        {
            Id = Guid.NewGuid(),
            CompanyName = "Winterfell",
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV"
        };
        List<InvoiceItemModel> invoiceItems =
        [
            new() { Id = Guid.NewGuid(), Name = "Item 1", Quantity = 1, Price = 10.5m, Comments = "Item 1 comment" },
            new() { Id = Guid.NewGuid(), Name = "Item 2", Quantity = 2, Price = 20.5m, Comments = "Item 2 comment" }
        ];

        var from = new InvoiceModelFull
        {
            Id = Guid.NewGuid(),
            UserId = userModel.Id,
            SellerId = sellerModel.Id,
            CustomerId = customerModel.Id,
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
            "SellerName",
            "CustomerName",
            "Items",
            "TotalPrice"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.SellerName.Should().Be("Lannister gold");
        to.CustomerName.Should().Be("Winterfell");
        to.Items.Should().BeEquivalentTo(invoiceItems.Select(i => i.ToInvoiceItemResponse()));
        to.TotalPrice.Should().Be(51.5m);
    }
}
