using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceValidatorTest
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ISellerRepository> _sellerRepositoryMock;
    private readonly Mock<IItemRepository> _itemRepositoryMock;
    private readonly InvoiceValidator _validator;

    public InvoiceValidatorTest()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _sellerRepositoryMock = new Mock<ISellerRepository>(MockBehavior.Strict);
        _itemRepositoryMock = new Mock<IItemRepository>(MockBehavior.Strict);

        _validator = new InvoiceValidator(_customerRepositoryMock.Object, _sellerRepositoryMock.Object, _itemRepositoryMock.Object);
    }

    public static IEnumerable<object[]> GetSellerTestData()
    {
        SellerEntity seller = new() { Id = Guid.NewGuid() };
        SellerEntity otherSeller = new() { Id = Guid.NewGuid() };

        // users seller
        yield return new object[] { new List<SellerEntity> { otherSeller, seller }, seller.Id, true };
        // other seller
        yield return new object[] { new List<SellerEntity> { seller }, Guid.NewGuid(), false };
        // no sellers
        yield return new object[] { new List<SellerEntity>(), Guid.NewGuid(), false };
    }

    [Theory]
    [MemberData(nameof(GetSellerTestData))]
    public async Task IsValidSellerId_GivenUsersSellers_ReturnsExpected(List<SellerEntity> sellers, Guid sellerId, bool expected)
    {
        //Arrange
        Guid userId = Guid.NewGuid();

        _sellerRepositoryMock.Setup(m => m.GetByUserId(userId))
                        .ReturnsAsync(sellers);

        //Act
        bool result = await _validator.IsValidSellerId(sellerId, userId);

        //Assert
        result.Should().Be(expected);

        _sellerRepositoryMock.Verify(m => m.GetByUserId(userId), Times.Once());
    }

    public static IEnumerable<object[]> GetCustomerTestData()
    {
        CustomerEntity customer = new() { Id = Guid.NewGuid() };
        CustomerEntity otherCustomer = new() { Id = Guid.NewGuid() };

        // sellers customer
        yield return new object[] { new List<CustomerEntity> { otherCustomer, customer }, customer.Id, true };
        // other customer
        yield return new object[] { new List<CustomerEntity> { customer }, Guid.NewGuid(), false };
        // no customers
        yield return new object[] { new List<CustomerEntity>(), Guid.NewGuid(), false };
    }

    [Theory]
    [MemberData(nameof(GetCustomerTestData))]
    public async Task IsValidCustomerId_GivenSellersCustomers_ReturnsExpected(List<CustomerEntity> customers, Guid customerId, bool expected)
    {
        //Arrange
        Guid sellerId = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.GetBySellerId(sellerId))
                        .ReturnsAsync(customers);

        //Act
        bool result = await _validator.IsValidCustomerId(customerId, sellerId);

        //Assert
        result.Should().Be(expected);

        _customerRepositoryMock.Verify(m => m.GetBySellerId(sellerId), Times.Once());
    }

    public static IEnumerable<object[]> GetItemsTestData()
    {
        ItemEntity firstItem = new() { Id = Guid.NewGuid() };
        ItemEntity secondItem = new() { Id = Guid.NewGuid() };
        ItemEntity unusedItem = new() { Id = Guid.NewGuid() };

        // subset of customers items
        yield return new object[] { new List<ItemEntity> { firstItem, unusedItem, secondItem }, new List<Guid> { firstItem.Id, secondItem.Id }, true };
        // one foreign item
        yield return new object[] { new List<ItemEntity> { firstItem }, new List<Guid> { firstItem.Id, Guid.NewGuid() }, false };
        // no customer items
        yield return new object[] { new List<ItemEntity>(), new List<Guid> { Guid.NewGuid() }, false };
    }

    [Theory]
    [MemberData(nameof(GetItemsTestData))]
    public async Task IsValidItemsId_GivenCustomersItems_ReturnsExpected(List<ItemEntity> items, List<Guid> itemIds, bool expected)
    {
        //Arrange
        Guid customerId = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.GetByCustomerId(customerId))
                        .ReturnsAsync(items);

        //Act
        bool result = await _validator.IsValidItemsId(itemIds, customerId);

        //Assert
        result.Should().Be(expected);

        _itemRepositoryMock.Verify(m => m.GetByCustomerId(customerId), Times.Once());
    }
}
