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
        SellerModel sellerModel = new()
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
        CustomerModel customerModel = new()
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
            InvoiceNumber = 9
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
            "Seller",
            "Customer",
            "Items",
            "TotalPrice"
        ];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        to.Seller.Should().Be(sellerModel.ToResponse());
        to.Customer.Should().Be(customerModel.ToResponse());
        to.Items.Should().BeEquivalentTo(invoiceItems.Select(i => i.ToInvoiceItemResponse()));
        to.TotalPrice.Should().Be(51.5m);
    }
}
