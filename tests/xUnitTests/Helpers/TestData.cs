using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using Domain.Entities;

namespace xUnitTests.Helpers;

/// <summary>
/// Hand-built test data. Every property is non-default and every call gets new ids,
/// so lists built from these hold distinct entities.
/// </summary>
public static class TestData
{
    public static UserEntity NewUserEntity() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jon",
        LastName = "Snow",
        Email = "jon.snow@winterfell.com",
        Password = "ghost",
        Salt = "salt"
    };

    public static UserModel NewUserModel() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jon",
        LastName = "Snow",
        Email = "jon.snow@winterfell.com",
        Password = "ghost"
    };

    public static SellerEntity NewSellerEntity(Guid? userId = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        CompanyName = "Iron Bank",
        CompanyNumber = "IB-001",
        Street = "Bank street 1",
        City = "Braavos",
        State = "Free Cities",
        Email = "info@ironbank.com",
        Phone = "+370 600 00001",
        BankName = "Iron Bank",
        BankNumber = "LT000000000000000001"
    };

    public static SellerModel NewSellerModel(Guid? userId = null) => NewSellerEntity(userId).ToModel();

    public static CustomerEntity NewCustomerEntity(Guid? sellerId = null) => new()
    {
        Id = Guid.NewGuid(),
        SellerId = sellerId ?? Guid.NewGuid(),
        InvoiceName = "WNT",
        InvoiceNumber = 7,
        CompanyName = "Winterfell",
        CompanyNumber = "WF-001",
        Street = "Great hall 1",
        City = "Winterfell",
        State = "The North",
        Email = "ned@winterfell.com",
        Phone = "+370 600 00002"
    };

    public static CustomerModel NewCustomerModel(Guid? sellerId = null) => NewCustomerEntity(sellerId).ToModel();

    public static ItemEntity NewItemEntity(Guid? customerId = null) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId ?? Guid.NewGuid(),
        Name = "Iron throne",
        Price = 100.50m,
        Quantity = 2
    };

    public static ItemModel NewItemModel(Guid? customerId = null) => NewItemEntity(customerId).ToModel();

    public static InvoiceItemModel NewInvoiceItemModel() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Iron throne",
        Price = 100.50m,
        Quantity = 2,
        Comments = "Slightly used"
    };

    public static InvoiceModel NewInvoiceModel()
    {
        UserModel user = NewUserModel();
        SellerModel seller = NewSellerModel(user.Id);
        CustomerModel customer = NewCustomerModel(seller.Id);

        return new InvoiceModel
        {
            Id = Guid.NewGuid(),
            User = user,
            Seller = seller,
            Customer = customer,
            Items = [NewInvoiceItemModel(), NewInvoiceItemModel(), NewInvoiceItemModel()],
            InvoiceNumber = customer.InvoiceNumber,
            Comments = "Winter is coming",
            Status = InvoiceStatus.Created,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5))
        };
    }

    public static InvoiceEntity NewInvoiceEntity() => NewInvoiceModel().ToEntity();
}
