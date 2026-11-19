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

        _itemRepositoryMock.Setup(m => m.Get(item.Id, null))
                        .ReturnsAsync(item);

        ItemModel expectedResult = item.ToModel();

        //Act
        ItemModel result = await _itemService.Get(item.Id, null);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(item.Id, null), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(id, null))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Get(id, null))
                            .Should().ThrowAsync<NotFoundException>().WithMessage($"Invoice item:{id} not found");

        _itemRepositoryMock.Verify(m => m.Get(id, null), Times.Once());
    }

    [Fact]
    public async Task GetIdVersion_GivenExistingVersion_ReturnsThatVersion()
    {
        //Arrange
        ItemEntity item = new() { Id = Guid.NewGuid(), Version = 2, Name = "Iron throne", Price = 50m };

        _itemRepositoryMock.Setup(m => m.Get(item.Id, 2))
                        .ReturnsAsync(item);

        //Act
        ItemModel result = await _itemService.Get(item.Id, 2);

        //Assert
        result.Should().BeEquivalentTo(item.ToModel());

        _itemRepositoryMock.Verify(m => m.Get(item.Id, 2), Times.Once());
    }

    [Fact]
    public async Task GetIdVersion_GivenUnknownVersion_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(id, 99))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Get(id, 99))
                            .Should().ThrowAsync<NotFoundException>().WithMessage($"Invoice item:{id} version 99 not found");

        _itemRepositoryMock.Verify(m => m.Get(id, 99), Times.Once());
    }

    [Fact]
    public async Task GetVersions_GivenItemWithVersions_ReturnsThemInRepositoryOrder()
    {
        //Arrange
        Guid id = Guid.NewGuid();
        List<ItemVersionEntity> versions = [new() { ItemId = id, Version = 1 }, new() { ItemId = id, Version = 2 }];

        _itemRepositoryMock.Setup(m => m.GetVersions(id))
                        .ReturnsAsync(versions);

        //Act
        var result = await _itemService.GetVersions(id);

        //Assert
        result.Select(v => v.Version).Should().Equal(1, 2);

        _itemRepositoryMock.Verify(m => m.GetVersions(id), Times.Once());
    }

    [Fact]
    public async Task GetVersions_GivenUnknownItem_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.GetVersions(id))
                        .ReturnsAsync((List<ItemVersionEntity>)[]);

        //Act
        //Assert
        await _itemService.Invoking(x => x.GetVersions(id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.GetVersions(id), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<int?>> GetValidLineVersionTestData()
    {
        yield return new((int?)null) { Label = "line without a version is valid, it means the latest" };
        yield return new(1) { Label = "line with an older version of the item is valid" };
        yield return new(2) { Label = "line with the latest version of the item is valid" };
    }

    [Theory]
    [MemberData(nameof(GetValidLineVersionTestData))]
    public async Task GetVersionsWithValidation_GivenExistingVersion_ReturnsItemVersions(int? lineVersion)
    {
        //Arrange
        Guid customerId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        List<InvoiceItemModel> lines = [new() { Id = itemId, Version = lineVersion }];
        List<ItemVersionEntity> versions =
        [
            new() { ItemId = itemId, CustomerId = customerId, Version = 1 },
            new() { ItemId = itemId, CustomerId = customerId, Version = 2 }
        ];

        _itemRepositoryMock.Setup(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { itemId }))))
                        .ReturnsAsync(versions);

        //Act
        var resultResponse = await _itemService.GetVersionsWithValidation(lines, customerId);
        IEnumerable<ItemVersionModel> result = resultResponse.Match(
            versions => { return versions; },
            error => { throw new Exception(error.ToString()); }
        );

        //Assert
        result.Should().BeEquivalentTo(versions.Select(v => v.ToModel()));

        _itemRepositoryMock.Verify(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { itemId }))), Times.Once());
    }

    [Fact]
    public async Task GetVersionsWithValidation_GivenOtherCustomersItem_ReturnsErrorModel()
    {
        //Arrange
        Guid customerId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        List<InvoiceItemModel> lines = [new() { Id = itemId }];
        List<ItemVersionEntity> versions = [new() { ItemId = itemId, CustomerId = Guid.NewGuid(), Version = 1 }];

        _itemRepositoryMock.Setup(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { itemId }))))
                        .ReturnsAsync(versions);

        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Items id {itemId} is invalid for customer id {customerId}"
        };

        //Act
        var resultResponse = await _itemService.GetVersionsWithValidation(lines, customerId);
        ErrorModel result = resultResponse.Match(
            versions => { throw new Exception("Got versions not error" + versions.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { itemId }))), Times.Once());
    }

    [Fact]
    public async Task GetVersionsWithValidation_GivenUnknownItems_ReturnsErrorModelNamingEach()
    {
        //Arrange
        Guid customerId = Guid.NewGuid();
        List<InvoiceItemModel> lines = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];
        List<Guid> lineIds = lines.Select(l => l.Id).ToList();

        _itemRepositoryMock.Setup(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(lineIds))))
                        .ReturnsAsync((List<ItemVersionEntity>)[]);

        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Items id {lineIds[0]} {lineIds[1]} is invalid for customer id {customerId}"
        };

        //Act
        var resultResponse = await _itemService.GetVersionsWithValidation(lines, customerId);
        ErrorModel result = resultResponse.Match(
            versions => { throw new Exception("Got versions not error" + versions.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(lineIds))), Times.Once());
    }

    [Fact]
    public async Task GetVersionsWithValidation_GivenVersionOnlyAnotherLinesItemHas_ReturnsErrorModel()
    {
        //Arrange
        Guid customerId = Guid.NewGuid();
        Guid throneId = Guid.NewGuid();
        Guid swordId = Guid.NewGuid();
        List<InvoiceItemModel> lines = [new() { Id = throneId, Version = 3 }, new() { Id = swordId, Version = 1 }];
        List<ItemVersionEntity> versions =
        [
            new() { ItemId = throneId, CustomerId = customerId, Version = 1 },
            new() { ItemId = swordId, CustomerId = customerId, Version = 1 },
            new() { ItemId = swordId, CustomerId = customerId, Version = 2 },
            new() { ItemId = swordId, CustomerId = customerId, Version = 3 }
        ];

        _itemRepositoryMock.Setup(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { throneId, swordId }))))
                        .ReturnsAsync(versions);

        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Item id {throneId} has no version 3"
        };

        //Act
        var resultResponse = await _itemService.GetVersionsWithValidation(lines, customerId);
        ErrorModel result = resultResponse.Match(
            versions => { throw new Exception("Got versions not error" + versions.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { throneId, swordId }))), Times.Once());
    }

    [Fact]
    public async Task GetVersionsWithValidation_GivenUnknownVersion_ReturnsErrorModel()
    {
        //Arrange
        Guid customerId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        List<InvoiceItemModel> lines = [new() { Id = itemId, Version = 99 }];
        List<ItemVersionEntity> versions = [new() { ItemId = itemId, CustomerId = customerId, Version = 1 }];

        _itemRepositoryMock.Setup(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { itemId }))))
                        .ReturnsAsync(versions);

        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Item id {itemId} has no version 99"
        };

        //Act
        var resultResponse = await _itemService.GetVersionsWithValidation(lines, customerId);
        ErrorModel result = resultResponse.Match(
            versions => { throw new Exception("Got versions not error" + versions.ToString()); },
            error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.GetVersions(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { itemId }))), Times.Once());
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

        _itemRepositoryMock.Setup(m => m.Get(itemEntity.Id, null))
                                .ReturnsAsync(itemEntity);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Update(item))
                                        .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(item.Id, null), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(itemEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        ItemModel item = new() { Id = Guid.NewGuid() };

        _itemRepositoryMock.Setup(m => m.Get(item.Id, null))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Update(item))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(item.Id, null), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(It.IsAny<ItemEntity>()), Times.Never());
    }

    [Fact]
    public async Task Delete_GivenValidId_DeletesFromRepository()
    {
        //Arrange
        ItemEntity item = new() { Id = Guid.NewGuid() };

        _itemRepositoryMock.Setup(m => m.Delete(item.Id))
                        .Returns(Task.CompletedTask);

        _itemRepositoryMock.Setup(m => m.Get(item.Id, null))
                        .ReturnsAsync(item);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(item.Id, null), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(item.Id), Times.Once());
    }

    [Fact]
    public async Task Delete_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _itemRepositoryMock.Setup(m => m.Get(id, null))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(id, null), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }
}
