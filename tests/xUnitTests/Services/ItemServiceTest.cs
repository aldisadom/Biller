using Application.Models;
using Application.Services;
using AutoFixture;
using AutoFixture.Xunit2;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace xUnitTests.Services;

public class ItemServiceTest
{
    private readonly Mock<IInvoiceItemRepository> _itemRepositoryMock;
    private readonly InvoiceItemService _itemService;

    public ItemServiceTest()
    {
        _itemRepositoryMock = new Mock<IInvoiceItemRepository>();
        _itemService = new InvoiceItemService(_itemRepositoryMock.Object);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(Guid id)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync(new InvoiceItemEntity { Id = id });

        //Act
        InvoiceItemModel result = await _itemService.Get(id);

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
                        .ReturnsAsync((InvoiceItemEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.Get(id));

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsDTO()
    {
        int quantity = 5;

        Fixture _fixture = new();
        List<InvoiceItemEntity> itemList = [];
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
                        .ReturnsAsync(new List<InvoiceItemEntity>());

        // Act Assert
        var result = await _itemService.Get();

        result.Count().Should().Be(0);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(InvoiceItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Add(It.Is<InvoiceItemEntity>
                                (x => x.Name == item.Name && x.Price == item.Price)))
                                 .ReturnsAsync(item.Id);

        //Act
        InvoiceItemModel itemAdd = new()
        {
            Name = item.Name,
            Price = item.Price
        };

        Guid result = await _itemService.Add(itemAdd);

        //Assert
        result.Should().Be(item.Id);

        _itemRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(InvoiceItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Update(It.Is<InvoiceItemEntity>
                                (x => x.Id == item.Id && x.Name == item.Name && x.Price == item.Price)));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                                .ReturnsAsync(item);

        //Act
        InvoiceItemModel itemAdd = new()
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price
        };

        //Assert
        await _itemService.Invoking(x => x.Update(itemAdd))
                                        .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(InvoiceItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Update(It.Is<InvoiceItemEntity>
                                (x => x.Id == item.Id && x.Name == item.Name && x.Price == item.Price)));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync((InvoiceItemEntity)null!);

        //Act
        InvoiceItemModel itemAdd = new()
        {
            Id = item.Id,
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
    public async Task Delete_ValidId(InvoiceItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Delete(item.Id));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync(new InvoiceItemEntity { Id = item.Id }!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(InvoiceItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Delete(item.Id));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync((InvoiceItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}