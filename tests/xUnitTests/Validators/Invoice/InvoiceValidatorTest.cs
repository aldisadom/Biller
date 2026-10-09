using AutoFixture.Xunit2;
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

    [Theory]
    [AutoData]
    public async Task IsValidSellerId_GivenUsersSeller_ReturnsTrue(List<SellerEntity> sellers, Guid userId)
    {
        //Arrange
        _sellerRepositoryMock.Setup(m => m.GetByUserId(userId))
                        .ReturnsAsync(sellers);

        //Act
        bool result = await _validator.IsValidSellerId(sellers[1].Id, userId);

        //Assert
        result.Should().BeTrue();

        _sellerRepositoryMock.Verify(m => m.GetByUserId(userId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidSellerId_GivenOtherSeller_ReturnsFalse(List<SellerEntity> sellers, Guid userId, Guid sellerId)
    {
        //Arrange
        _sellerRepositoryMock.Setup(m => m.GetByUserId(userId))
                        .ReturnsAsync(sellers);

        //Act
        bool result = await _validator.IsValidSellerId(sellerId, userId);

        //Assert
        result.Should().BeFalse();

        _sellerRepositoryMock.Verify(m => m.GetByUserId(userId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidSellerId_GivenNoSellers_ReturnsFalse(Guid userId, Guid sellerId)
    {
        //Arrange
        _sellerRepositoryMock.Setup(m => m.GetByUserId(userId))
                        .ReturnsAsync([]);

        //Act
        bool result = await _validator.IsValidSellerId(sellerId, userId);

        //Assert
        result.Should().BeFalse();

        _sellerRepositoryMock.Verify(m => m.GetByUserId(userId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidCustomerId_GivenSellersCustomer_ReturnsTrue(List<CustomerEntity> customers, Guid sellerId)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.GetBySellerId(sellerId))
                        .ReturnsAsync(customers);

        //Act
        bool result = await _validator.IsValidCustomerId(customers[0].Id, sellerId);

        //Assert
        result.Should().BeTrue();

        _customerRepositoryMock.Verify(m => m.GetBySellerId(sellerId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidCustomerId_GivenOtherCustomer_ReturnsFalse(List<CustomerEntity> customers, Guid sellerId, Guid customerId)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.GetBySellerId(sellerId))
                        .ReturnsAsync(customers);

        //Act
        bool result = await _validator.IsValidCustomerId(customerId, sellerId);

        //Assert
        result.Should().BeFalse();

        _customerRepositoryMock.Verify(m => m.GetBySellerId(sellerId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidItemsId_GivenAllCustomersItems_ReturnsTrue(List<ItemEntity> items, Guid customerId)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.GetByCustomerId(customerId))
                        .ReturnsAsync(items);

        List<Guid> itemIds = [items[0].Id, items[2].Id];

        //Act
        bool result = await _validator.IsValidItemsId(itemIds, customerId);

        //Assert
        result.Should().BeTrue();

        _itemRepositoryMock.Verify(m => m.GetByCustomerId(customerId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidItemsId_GivenOneForeignItem_ReturnsFalse(List<ItemEntity> items, Guid customerId, Guid foreignItemId)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.GetByCustomerId(customerId))
                        .ReturnsAsync(items);

        List<Guid> itemIds = [items[0].Id, foreignItemId];

        //Act
        bool result = await _validator.IsValidItemsId(itemIds, customerId);

        //Assert
        result.Should().BeFalse();

        _itemRepositoryMock.Verify(m => m.GetByCustomerId(customerId), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task IsValidItemsId_GivenNoCustomerItems_ReturnsFalse(Guid customerId, Guid itemId)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.GetByCustomerId(customerId))
                        .ReturnsAsync([]);

        //Act
        bool result = await _validator.IsValidItemsId([itemId], customerId);

        //Assert
        result.Should().BeFalse();

        _itemRepositoryMock.Verify(m => m.GetByCustomerId(customerId), Times.Once());
    }
}
