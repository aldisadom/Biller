using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Item;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using AwesomeAssertions;
using Moq;
using System.Net;

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
        ItemEntity item = new() { Id = Guid.NewGuid() };

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
        //Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Get(id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task GetIds_GivenValidIds_ReturnsDTOs()
    {
        //Arrange
        List<ItemEntity> itemList = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

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
        List<ItemEntity> itemList = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

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
        //Arrange
        List<ItemEntity> itemList = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

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

        //Act
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
        //Arrange
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

        //Act
        var resultResponse = await _itemService.GetWithValidation(ids, customerId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _itemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<ItemGetRequest?, Guid?, int, int>> GetQueryTestData()
    {
        Guid customerId = Guid.NewGuid();

        yield return new(null, null, 1, 25) { Label = "no query reads the first 25 items of all customers" };
        yield return new(new ItemGetRequest(), null, 1, 25) { Label = "empty query reads the first 25 items of all customers" };
        yield return new(new ItemGetRequest { CustomerId = customerId, Page = 2, PageSize = PageSize.p50 }, customerId, 2, 50) { Label = "customer filter and paging are passed to the repository" };
    }

    [Theory]
    [MemberData(nameof(GetQueryTestData))]
    public async Task Get_GivenQuery_PassesFilterToRepository(ItemGetRequest? query, Guid? customerId, int page, int pageSize)
    {
        //Arrange
        List<ItemEntity> items = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

        _itemRepositoryMock.Setup(m => m.Get(customerId, page, pageSize))
                        .ReturnsAsync((items, 9, page));

        //Act
        var (result, totalCount, returnedPage) = await _itemService.Get(query);

        //Assert
        result.Select(i => i.Id).Should().Equal(items.Select(i => i.Id));
        totalCount.Should().Be(9);
        returnedPage.Should().Be(page);

        _itemRepositoryMock.Verify(m => m.Get(customerId, page, pageSize), Times.Once());
    }

    [Fact]
    public async Task Get_GivenPagePastTheEnd_ReturnsPageTheRepositoryReturned()
    {
        //Arrange
        ItemGetRequest query = new() { Page = 5 };
        List<ItemEntity> items = [new() { Id = Guid.NewGuid() }];

        _itemRepositoryMock.Setup(m => m.Get(null, 5, 25))
                        .ReturnsAsync((items, 4, 1));

        //Act
        var (result, totalCount, returnedPage) = await _itemService.Get(query);

        //Assert
        result.Select(i => i.Id).Should().Equal(items.Select(i => i.Id));
        totalCount.Should().Be(4);
        returnedPage.Should().Be(1);

        _itemRepositoryMock.Verify(m => m.Get(null, 5, 25), Times.Once());
    }

    [Fact]
    public async Task Add_GivenValidId_ReturnsGuid()
    {
        //Arrange
        ItemModel item = new() { Id = Guid.NewGuid() };

        ItemEntity itemEntity = item.ToEntity();

        _itemRepositoryMock.Setup(m => m.Add(itemEntity))
                                 .ReturnsAsync(item.Id);

        //Act
        Guid result = await _itemService.Add(item);

        //Assert
        result.Should().Be(item.Id);

        _itemRepositoryMock.Verify(m => m.Add(itemEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenExistingId_UpdatesRepository()
    {
        //Arrange
        ItemModel item = new() { Id = Guid.NewGuid() };

        ItemEntity itemEntity = item.ToEntity();

        _itemRepositoryMock.Setup(m => m.Update(itemEntity))
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
    public async Task Update_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        ItemModel item = new() { Id = Guid.NewGuid() };

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
    public async Task Delete_GivenValidId_DeletesFromRepository()
    {
        //Arrange
        ItemEntity item = new() { Id = Guid.NewGuid() };

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
    public async Task Delete_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

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
