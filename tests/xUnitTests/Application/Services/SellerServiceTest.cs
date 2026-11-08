using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Seller;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using AwesomeAssertions;
using Moq;
using System.Net;

namespace xUnitTests.Application.Services;

public class SellerServiceTest
{
    private readonly Mock<ISellerRepository> _sellerRepositoryMock;
    private readonly SellerService _sellerService;

    public SellerServiceTest()
    {
        _sellerRepositoryMock = new Mock<ISellerRepository>(MockBehavior.Strict);

        _sellerService = new SellerService(_sellerRepositoryMock.Object);
    }

    [Fact]
    public async Task GetId_GivenValidId_ReturnsDTO()
    {
        //Arrange
        SellerEntity seller = new() { Id = Guid.NewGuid() };

        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync(seller);

        SellerModel expectedResult = seller.ToModel();

        //Act
        SellerModel result = await _sellerService.Get(seller.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _sellerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SellerEntity)null!);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Get(id))
                            .Should().ThrowAsync<NotFoundException>();

        _sellerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task GetIdWithValidation_GivenValidId_ReturnsDTO()
    {
        //Arrange
        SellerEntity seller = new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };

        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync(seller);

        SellerModel expectedResult = seller.ToModel();

        //Act
        var resultResponse = await _sellerService.GetWithValidation(seller.Id, seller.UserId);
        SellerModel result = resultResponse.Match(
            entity => { return entity; },
            error => { throw new Exception(error.ToString()); }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
    }

    [Fact]
    public async Task GetIdWithValidation_GivenInvaliduserId_ReturnsErrorModel()
    {
        //Arrange
        SellerEntity seller = new() { Id = Guid.NewGuid() };

        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync(seller);

        var userId = Guid.NewGuid();
        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Seller id {seller.Id} is invalid for user id {userId}"
        };

        //Act
        var resultResponse = await _sellerService.GetWithValidation(seller.Id, userId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
        error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
    }

    [Fact]
    public async Task GetIdWithValidation_GivenInvalidId_ReturnsErrorModel()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _sellerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SellerEntity)null!);

        var userId = Guid.NewGuid();
        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Seller id {id} is invalid for user id {userId}"
        };

        //Act
        var resultResponse = await _sellerService.GetWithValidation(id, userId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
        error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _sellerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<SellerGetRequest?, Guid?, int, int>> GetQueryTestData()
    {
        Guid userId = Guid.NewGuid();

        yield return new(null, null, 1, 25) { Label = "no query reads the first 25 sellers of all users" };
        yield return new(new SellerGetRequest(), null, 1, 25) { Label = "empty query reads the first 25 sellers of all users" };
        yield return new(new SellerGetRequest { UserId = userId, Page = 2, PageSize = PageSize.p50 }, userId, 2, 50) { Label = "user filter and paging are passed to the repository" };
    }

    [Theory]
    [MemberData(nameof(GetQueryTestData))]
    public async Task Get_GivenQuery_PassesFilterToRepository(SellerGetRequest? query, Guid? userId, int page, int pageSize)
    {
        //Arrange
        List<SellerEntity> sellers = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

        _sellerRepositoryMock.Setup(m => m.Get(userId, page, pageSize))
                        .ReturnsAsync((sellers, 9, page));

        //Act
        var (result, totalCount, returnedPage) = await _sellerService.Get(query);

        //Assert
        result.Select(s => s.Id).Should().Equal(sellers.Select(s => s.Id));
        totalCount.Should().Be(9);
        returnedPage.Should().Be(page);

        _sellerRepositoryMock.Verify(m => m.Get(userId, page, pageSize), Times.Once());
    }

    [Fact]
    public async Task Get_GivenPagePastTheEnd_ReturnsPageTheRepositoryReturned()
    {
        //Arrange
        SellerGetRequest query = new() { Page = 5 };
        List<SellerEntity> sellers = [new() { Id = Guid.NewGuid() }];

        _sellerRepositoryMock.Setup(m => m.Get(null, 5, 25))
                        .ReturnsAsync((sellers, 4, 1));

        //Act
        var (result, totalCount, returnedPage) = await _sellerService.Get(query);

        //Assert
        result.Select(s => s.Id).Should().Equal(sellers.Select(s => s.Id));
        totalCount.Should().Be(4);
        returnedPage.Should().Be(1);

        _sellerRepositoryMock.Verify(m => m.Get(null, 5, 25), Times.Once());
    }

    [Fact]
    public async Task Add_GivenValidId_ReturnsGuid()
    {
        //Arrange
        SellerModel seller = new() { Id = Guid.NewGuid() };

        SellerEntity sellerEntity = seller.ToEntity();

        _sellerRepositoryMock.Setup(m => m.Add(sellerEntity))
                                 .ReturnsAsync(seller.Id);

        //Act
        Guid result = await _sellerService.Add(seller);

        //Assert
        result.Should().Be(seller.Id);

        _sellerRepositoryMock.Verify(m => m.Add(sellerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenExistingId_UpdatesRepository()
    {
        //Arrange
        SellerModel seller = new() { Id = Guid.NewGuid() };

        SellerEntity sellerEntity = seller.ToEntity();

        _sellerRepositoryMock.Setup(m => m.Update(sellerEntity))
                        .Returns(Task.CompletedTask);

        _sellerRepositoryMock.Setup(m => m.Get(sellerEntity.Id))
                                .ReturnsAsync(sellerEntity);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Update(seller))
                                        .Should().NotThrowAsync<Exception>();

        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Update(sellerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        SellerModel seller = new() { Id = Guid.NewGuid() };

        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync((SellerEntity)null!);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Update(seller))
                            .Should().ThrowAsync<NotFoundException>();

        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Update(It.IsAny<SellerEntity>()), Times.Never());
    }

    [Fact]
    public async Task Delete_GivenValidId_DeletesFromRepository()
    {
        //Arrange
        SellerEntity seller = new() { Id = Guid.NewGuid() };

        _sellerRepositoryMock.Setup(m => m.Delete(seller.Id))
                        .Returns(Task.CompletedTask);

        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync(seller);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Delete(seller.Id))
                            .Should().NotThrowAsync<Exception>();

        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Delete(seller.Id), Times.Once());
    }

    [Fact]
    public async Task Delete_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _sellerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SellerEntity)null!);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _sellerRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }
}
