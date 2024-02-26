using Application.Models;
using Application.Services;
using AutoFixture;
using AutoFixture.Xunit2;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ShopV2.UnitTest.Services;

public class ItemServiceTest
{
    private readonly Mock<IItemRepository> _itemRepositoryMock;
    private readonly ItemService _itemService;

    public ItemServiceTest()
    {
        _itemRepositoryMock = new Mock<IItemRepository>();
        _itemService = new ItemService(_itemRepositoryMock.Object);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(Guid id)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync(new ItemEntity { Id = id });

        //Act
        ItemModel result = await _itemService.Get(id);

        //Assert
        result.Id.Should().Be(id);

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((ItemEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.Get(id));

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsDTO()
    {
        int quantity = 5;

        Fixture _fixture = new();
        List<ItemEntity> itemList = [];
        _fixture.AddManyTo(itemList, quantity);

        //Arrange
        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(itemList);

        //Act
        var result = await _itemService.Get();

        //Assert
        result.Count().Should().Be(quantity);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(new List<ItemEntity>());

        // Act Assert
        var result = await _itemService.Get();

        result.Count().Should().Be(0);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Add(It.Is<ItemEntity>
                                (x => x.Name == item.Name && x.Price == item.Price)))
                                 .ReturnsAsync(item.Id);

        //Act
        ItemModel itemAdd = new()
        {
            Name = item.Name,
            Price = item.Price
        };

        Guid result = await _itemService.Add(itemAdd);

        //Assert
        result.Should().Be(item.Id);

        _itemRepositoryMock.Verify(m => m.Add(It.IsAny<ItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Update(It.Is<ItemEntity>
                                (x => x.Id == item.Id && x.Name == item.Name && x.Price == item.Price)))
                                .ReturnsAsync(1);

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                                .ReturnsAsync(item);

        //Act
        ItemModel itemAdd = new()
        {
            Id = item.Id,
            ShopId = item.ShopId,
            Name = item.Name,
            Price = item.Price
        };

        //Assert
        await _itemService.Invoking(x => x.Update(itemAdd))
                                        .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(It.IsAny<ItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Update(It.Is<ItemEntity>
                                (x => x.Id == item.Id && x.Name == item.Name && x.Price == item.Price)))
                                .ReturnsAsync(1);

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        ItemModel itemAdd = new()
        {
            Id = item.Id,
            ShopId = item.ShopId,
            Name = item.Name,
            Price = item.Price
        };

        //Assert
        await _itemService.Invoking(x => x.Update(itemAdd))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Delete(item.Id));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync(new ItemEntity { Id = item.Id }!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Delete(item.Id));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}