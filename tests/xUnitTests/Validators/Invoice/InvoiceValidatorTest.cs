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

    public static IEnumerable<TheoryDataRow<List<SellerEntity>, Guid, bool>> GetSellerTestData()
    {
        SellerEntity seller = new() { Id = Guid.NewGuid() };
        SellerEntity otherSeller = new() { Id = Guid.NewGuid() };

        yield return new([otherSeller, seller], seller.Id, true) { Label = "seller id is one of the user's sellers, returns true" };
        yield return new([seller], Guid.NewGuid(), false) { Label = "seller id belongs to another user, returns false" };
        yield return new([], Guid.NewGuid(), false) { Label = "user has no sellers, returns false" };
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

    public static IEnumerable<TheoryDataRow<List<CustomerEntity>, Guid, bool>> GetCustomerTestData()
    {
        CustomerEntity customer = new() { Id = Guid.NewGuid() };
        CustomerEntity otherCustomer = new() { Id = Guid.NewGuid() };

        yield return new([otherCustomer, customer], customer.Id, true) { Label = "customer id is one of the seller's customers, returns true" };
        yield return new([customer], Guid.NewGuid(), false) { Label = "customer id belongs to another seller, returns false" };
        yield return new([], Guid.NewGuid(), false) { Label = "seller has no customers, returns false" };
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

    public static IEnumerable<TheoryDataRow<List<ItemEntity>, List<Guid>, bool>> GetItemsTestData()
    {
        ItemEntity firstItem = new() { Id = Guid.NewGuid() };
        ItemEntity secondItem = new() { Id = Guid.NewGuid() };
        ItemEntity unusedItem = new() { Id = Guid.NewGuid() };

        yield return new([firstItem, unusedItem, secondItem], [firstItem.Id, secondItem.Id], true) { Label = "all item ids are among the customer's items, returns true" };
        yield return new([firstItem], [firstItem.Id, Guid.NewGuid()], false) { Label = "one item id is not among the customer's items, returns false" };
        yield return new([], [Guid.NewGuid()], false) { Label = "customer has no items, returns false" };
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
