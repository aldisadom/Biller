using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.Customer;
using Contracts.Requests.Seller;
using Contracts.Responses.Customer;
using Contracts.Responses.Seller;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.SwaggerExamples.Customer;
using WebAPI.SwaggerExamples.Seller;

namespace xUnitTests.Services;

public class SellerServiceTest
{
    private readonly Mock<ISellerRepository> _sellerRepositoryMock;
    private readonly SellerService _sellerService;
    private readonly IMapper _mapper;

    public SellerServiceTest()
    {
        _sellerRepositoryMock = new Mock<ISellerRepository>();

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

        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
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

        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<SellerEntity> sellerList)
    {
        //Arrange
        SellerGetRequest request = new();

        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(sellerList);

        _sellerRepositoryMock.Setup(m => m.GetByUser(It.IsAny<Guid>()))
                        .ReturnsAsync((List<SellerEntity>)null!);

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
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(SellerGetRequest request, List<SellerEntity> sellerList)
    {
        //Arrange

        _sellerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<SellerEntity>)null!);

        _sellerRepositoryMock.Setup(m => m.GetByUser((Guid)request.UserId!))
                        .ReturnsAsync(sellerList);

        List<SellerModel> expectedResult = _mapper.Map<List<SellerModel>>(sellerList);

        //Act
        var result = await _sellerService.Get(request);

        //Assert
        result.Count().Should().Be(sellerList.Count);

        _sellerRepositoryMock.Verify(m => m.Get(), Times.Never());
        _sellerRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        SellerGetRequest request = new();
        List<SellerEntity> sellerList = [];

        //Arrange
        _sellerRepositoryMock.Setup(m => m.GetByUser(It.IsAny<Guid>()))
                        .ReturnsAsync(sellerList);

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
        
        _sellerRepositoryMock.Setup(m => m.Add(It.Is<SellerEntity>
                                (x => x == sellerEntity)))
                                 .ReturnsAsync(seller.Id);

        //Act
        Guid result = await _sellerService.Add(seller);

        //Assert
        result.Should().Be(seller.Id);

        _sellerRepositoryMock.Verify(m => m.Add(It.IsAny<SellerEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(SellerModel seller)
    {
        //Arrange
        SellerEntity sellerEntity = _mapper.Map<SellerEntity>(seller);

        _sellerRepositoryMock.Setup(m => m.Update(It.Is<SellerEntity>
                                (x => x == sellerEntity)));

        _sellerRepositoryMock.Setup(m => m.Get(sellerEntity.Id))
                                .ReturnsAsync(sellerEntity);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Update(seller))
                                        .Should().NotThrowAsync<Exception>();

        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Update(It.IsAny<SellerEntity>()), Times.Once());
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

        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(SellerEntity seller)
    {
        //Arrange
        _sellerRepositoryMock.Setup(m => m.Delete(seller.Id));

        _sellerRepositoryMock.Setup(m => m.Get(seller.Id))
                        .ReturnsAsync(seller);

        //Act
        //Assert
        await _sellerService.Invoking(x => x.Delete(seller.Id))
                            .Should().NotThrowAsync<Exception>();

        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _sellerRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
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

        _sellerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public void SellerAddRequest_ExampleTest(SellerAddRequest seller)
    {
        //Arrange
        SellerAddRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();

        //Assert
        exampleValues.Should().BeOfType<SellerAddRequest>();
        seller.Should().BeOfType<SellerAddRequest>();
    }

    [Theory]
    [AutoData]
    public void SellerUpdateRequest_ExampleTest(SellerUpdateRequest seller)
    {
        //Arrange
        SellerUpdateRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();

        //Assert
        exampleValues.Should().BeOfType<SellerUpdateRequest>();
        seller.Should().BeOfType<SellerUpdateRequest>();
    }

    [Theory]
    [AutoData]
    public void SellerListResponse_ExampleTest(SellerListResponse seller)
    {
        //Arrange
        SellerListResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();

        //Assert
        exampleValues.Should().BeOfType<SellerListResponse>();
        seller.Should().BeOfType<SellerListResponse>();
    }

    [Theory]
    [AutoData]
    public void SellerResponse_ExampleTest(SellerResponse seller)
    {
        //Arrange
        SellerResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();

        //Assert
        exampleValues.Should().BeOfType<SellerResponse>();
        seller.Should().BeOfType<SellerResponse>();
    }
}
