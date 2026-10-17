using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Requests.Seller;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
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
        // Arrange
        Guid id = Guid.NewGuid();

        _sellerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SellerEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _sellerService.Get(id));

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
        // Arrange
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

        // Act
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
        // Arrange
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

        // Act
        var resultResponse = await _sellerService.GetWithValidation(id, userId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
        error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _sellerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task Get_GivenNoQuery_ReturnsDTO()
    {
        //Arrange
        List<SellerEntity> sellerList = [new(), new(), new()];

        SellerGetRequest? request = null;

        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(sellerList);

        var expectedResult = sellerList.Select(s => s.ToModel());

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _sellerRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Get_GivenEmptyQuery_ReturnsDTO()
    {
        //Arrange
        List<SellerEntity> sellerList = [new(), new(), new()];

        SellerGetRequest request = new();

        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(sellerList);

        var expectedResult = sellerList.Select(s => s.ToModel());

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _sellerRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO()
    {
        //Arrange
        List<SellerEntity> sellerList = [new(), new(), new()];

        SellerGetRequest? request = new()
        {
            UserId = new Guid()
        };

        _sellerRepositoryMock.Setup(m => m.GetByUserId((Guid)request.UserId!))
                        .ReturnsAsync(sellerList);

        var expectedResult = sellerList.Select(s => s.ToModel());

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);

        _sellerRepositoryMock.Verify(m => m.GetByUserId((Guid)request.UserId!), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        SellerGetRequest request = new();
        List<SellerEntity> sellerList = [];

        //Arrange
        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(sellerList);

        // Act Assert
        var result = await _sellerService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<SellerModel>());

        _sellerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _sellerRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenValidId_ReturnsGuid()
    {
        //Arrange
        SellerModel seller = new() { Id = Guid.NewGuid() };

        SellerEntity sellerEntity = seller.ToEntity();

        _sellerRepositoryMock.Setup(m => m.Add(It.Is<SellerEntity>(x => x == sellerEntity)))
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

        _sellerRepositoryMock.Setup(m => m.Update(It.Is<SellerEntity>(x => x == sellerEntity)))
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

        SellerEntity sellerEntity = seller.ToEntity();

        _sellerRepositoryMock.Setup(m => m.Update(It.Is<SellerEntity>
                                (x => x == sellerEntity)));

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

        _sellerRepositoryMock.Setup(m => m.Delete(id));

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
