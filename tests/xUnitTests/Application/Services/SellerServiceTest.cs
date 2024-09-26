using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.Seller;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Application.Services;

public class SellerServiceTest
{
    private readonly Mock<ISellerRepository> _sellerRepositoryMock;
    private readonly SellerService _sellerService;
    private readonly IMapper _mapper;

    public SellerServiceTest()
    {
        _sellerRepositoryMock = new Mock<ISellerRepository>(MockBehavior.Strict);

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new SellerMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _sellerService = new SellerService(_sellerRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(SellerEntity seller)
    {
        //Arrange
        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync(seller);

        SellerModel expectedResult = _mapper.Map<SellerModel>(seller);

        //Act
        SellerModel result = await _sellerService.Get(seller.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(seller.Id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _sellerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SellerEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _sellerService.Get(id));

        _sellerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenNoQuery_ReturnsDTO(List<SellerEntity> sellerList)
    {
        //Arrange
        SellerGetRequest? request = null;

        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(sellerList);

        List<SellerModel> expectedResult = _mapper.Map<List<SellerModel>>(sellerList);

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _sellerRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<SellerEntity> sellerList)
    {
        //Arrange
        SellerGetRequest request = new();

        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(sellerList);

        List<SellerModel> expectedResult = _mapper.Map<List<SellerModel>>(sellerList);

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _sellerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _sellerRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(List<SellerEntity> sellerList)
    {
        //Arrange
        SellerGetRequest? request = new()
        {
            UserId = new Guid()
        };

        _sellerRepositoryMock.Setup(m => m.GetByUser((Guid)request.UserId!))
                        .ReturnsAsync(sellerList);

        List<SellerModel> expectedResult = _mapper.Map<List<SellerModel>>(sellerList);

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);

        _sellerRepositoryMock.Verify(m => m.GetByUser((Guid)request.UserId!), Times.Once());
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
        _sellerRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(SellerModel seller)
    {
        //Arrange
        SellerEntity sellerEntity = _mapper.Map<SellerEntity>(seller);

        _sellerRepositoryMock.Setup(m => m.Add(It.Is<SellerEntity>(x => x == sellerEntity)))
                                 .ReturnsAsync(seller.Id);

        //Act
        Guid result = await _sellerService.Add(seller);

        //Assert
        result.Should().Be(seller.Id);

        _sellerRepositoryMock.Verify(m => m.Add(sellerEntity), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(SellerModel seller)
    {
        //Arrange
        SellerEntity sellerEntity = _mapper.Map<SellerEntity>(seller);

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

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(SellerModel seller)
    {
        //Arrange
        SellerEntity sellerEntity = _mapper.Map<SellerEntity>(seller);

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

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(SellerEntity seller)
    {
        //Arrange
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

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
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
