using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Requests.Item;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using System.Net;
using xUnitTests.Helpers;

namespace xUnitTests.Application.Services;

public class ItemServiceTest
{
    private readonly Mock<IItemRepository> _itemRepositoryMock;
    private readonly ItemService _itemService;

    public ItemServiceTest()
    {
        _itemRepositoryMock = new Mock<IItemRepository>(MockBehavior.Strict);

        _itemService = new ItemService(_itemRepositoryMock.Object);
    }

    [Fact]
    public async Task GetId_GivenValidId_ReturnsDTO()
    {
        //Arrange
        ItemEntity item = TestData.NewItemEntity();

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync(item);

        ItemModel expectedResult = item.ToModel();

        //Act
        ItemModel result = await _itemService.Get(item.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(item.Id), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((ItemEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.Get(id));

        _itemRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task GetIds_GivenValidIds_ReturnsDTOs()
    {
        //Arrange
        List<ItemEntity> itemList = [TestData.NewItemEntity(), TestData.NewItemEntity(), TestData.NewItemEntity()];

        List<Guid> ids = itemList.Select(x => x.Id).ToList();

        _itemRepositoryMock.Setup(m => m.Get(ids))
                        .ReturnsAsync(itemList);

        var expectedResult = itemList.Select(i => i.ToModel());

        //Act
        var result = await _itemService.Get(ids);

        //Assert
        result.Count().Should().Be(itemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
    }

    [Fact]
    public async Task GetIdsWithValidation_GivenValidId_ReturnsDTO()
    {
        //Arrange
        List<ItemEntity> itemList = [TestData.NewItemEntity(), TestData.NewItemEntity(), TestData.NewItemEntity()];

        List<Guid> ids = itemList.Select(x => x.Id).ToList();
        var customerId = Guid.NewGuid();

        foreach (var item in itemList)
            item.CustomerId = customerId;

        _itemRepositoryMock.Setup(m => m.Get(ids))
                        .ReturnsAsync(itemList);

        var expectedResult = itemList.Select(i => i.ToModel());

        //Act
        var resultResponse = await _itemService.GetWithValidation(ids, customerId);
        IEnumerable<ItemModel> result = resultResponse.Match(
            entity => { return entity; },
            error => { throw new Exception(error.ToString()); }
        );

        //Assert
        result.Count().Should().Be(itemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
    }

    [Fact]
    public async Task GetIdsWithValidation_GivenInvalidcustomerId_ReturnsErrorModel()
    {
        // Arrange
        List<ItemEntity> itemList = [TestData.NewItemEntity(), TestData.NewItemEntity(), TestData.NewItemEntity()];

        List<Guid> ids = itemList.Select(x => x.Id).ToList();
        var customerId = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(ids))
                        .ReturnsAsync(itemList);

        string itemsIds = String.Join(" ", ids);
        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Items id {itemsIds} is invalid for customer id {customerId}"
        };

        // Act
        var resultResponse = await _itemService.GetWithValidation(ids, customerId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _itemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
    }

    [Fact]
    public async Task GetIdsWithValidation_GivenInvalidId_ReturnsErrorModel()
    {
        // Arrange
        List<Guid> ids = new() { Guid.NewGuid() };
        var customerId = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(ids))
                        .ReturnsAsync((List<ItemEntity>)[]);

        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Items id {ids[0]} is invalid for customer id {customerId}"
        };

        // Act
        var resultResponse = await _itemService.GetWithValidation(ids, customerId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _itemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmptyQuery_ReturnsDTO()
    {
        //Arrange
        List<ItemEntity> itemList = [TestData.NewItemEntity(), TestData.NewItemEntity(), TestData.NewItemEntity()];

        ItemGetRequest request = new();

        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(itemList);

        var expectedResult = itemList.Select(i => i.ToModel());

        //Act
        var result = await _itemService.Get(request);

        //Assert
        result.Count().Should().Be(itemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Get_GivenNullQuery_ReturnsDTO()
    {
        //Arrange
        List<ItemEntity> itemList = [TestData.NewItemEntity(), TestData.NewItemEntity(), TestData.NewItemEntity()];

        ItemGetRequest? request = null;

        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(itemList);

        var expectedResult = itemList.Select(i => i.ToModel());

        //Act
        var result = await _itemService.Get(request);

        //Assert
        result.Count().Should().Be(itemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO()
    {
        //Arrange
        List<ItemEntity> itemList = [TestData.NewItemEntity(), TestData.NewItemEntity(), TestData.NewItemEntity()];

        ItemGetRequest? request = new()
        {
            CustomerId = new Guid()
        };

        _itemRepositoryMock.Setup(m => m.GetByCustomerId((Guid)request.CustomerId!))
                        .ReturnsAsync(itemList);

        var expectedResult = itemList.Select(i => i.ToModel());

        //Act
        var result = await _itemService.Get(request);

        //Assert
        result.Count().Should().Be(itemList.Count);

        _itemRepositoryMock.Verify(m => m.GetByCustomerId((Guid)request.CustomerId!), Times.Once());
        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        ItemGetRequest request = new();
        List<ItemEntity> itemList = [];

        //Arrange
        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(itemList);

        // Act Assert
        var result = await _itemService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<ItemModel>());

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenValidId_ReturnsGuid()
    {
        //Arrange
        ItemModel item = TestData.NewItemModel();

        ItemEntity itemEntity = item.ToEntity();

        _itemRepositoryMock.Setup(m => m.Add(It.Is<ItemEntity>(x => x == itemEntity)))
                                 .ReturnsAsync(item.Id);

        //Act
        Guid result = await _itemService.Add(item);

        //Assert
        result.Should().Be(item.Id);

        _itemRepositoryMock.Verify(m => m.Add(itemEntity), Times.Once());
    }

    [Fact]
    public async Task Update_ReturnsSuccess()
    {
        //Arrange
        ItemModel item = TestData.NewItemModel();

        ItemEntity itemEntity = item.ToEntity();

        _itemRepositoryMock.Setup(m => m.Update(It.Is<ItemEntity>(x => x == itemEntity)))
                        .Returns(Task.CompletedTask);

        _itemRepositoryMock.Setup(m => m.Get(itemEntity.Id))
                                .ReturnsAsync(itemEntity);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Update(item))
                                        .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(item.Id), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(itemEntity), Times.Once());
    }

    [Fact]
    public async Task Update_InvalidId_NotFoundException()
    {
        //Arrange
        ItemModel item = TestData.NewItemModel();

        ItemEntity itemEntity = item.ToEntity();

        _itemRepositoryMock.Setup(m => m.Update(It.Is<ItemEntity>
                                (x => x == itemEntity)));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Update(item))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(item.Id), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(It.IsAny<ItemEntity>()), Times.Never());
    }

    [Fact]
    public async Task Delete_ValidId()
    {
        //Arrange
        ItemEntity item = TestData.NewItemEntity();

        _itemRepositoryMock.Setup(m => m.Delete(item.Id))
                        .Returns(Task.CompletedTask);

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync(item);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(item.Id), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(item.Id), Times.Once());
    }

    [Fact]
    public async Task Delete_InvalidId_ThrowNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Delete(id));

        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }
}
