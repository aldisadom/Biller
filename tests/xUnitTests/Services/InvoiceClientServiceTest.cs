using Application.Models;
using Application.Services;
using AutoFixture;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.InvoiceClient;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Services;

public class InvoiceClientServiceTest
{
    private readonly Mock<IInvoiceClientRepository> _invoiceClientRepositoryMock;
    private readonly InvoiceClientService _invoiceClientService;
    private readonly IMapper _mapper;

    public InvoiceClientServiceTest()
    {
        _invoiceClientRepositoryMock = new Mock<IInvoiceClientRepository>();

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new InvoiceClientMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _invoiceClientService = new InvoiceClientService(_invoiceClientRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(InvoiceClientEntity invoiceClient)
    {
        //Arrange
        _invoiceClientRepositoryMock.Setup(m => m.Get(invoiceClient.Id))
                        .ReturnsAsync(invoiceClient);

        InvoiceClientModel expectedResult = _mapper.Map<InvoiceClientModel>(invoiceClient);

        //Act
        InvoiceClientModel result = await _invoiceClientService.Get(invoiceClient.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceClientRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _invoiceClientRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceClientEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _invoiceClientService.Get(id));

        _invoiceClientRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<InvoiceClientEntity> invoiceClientList)
    {
        //Arrange
        InvoiceClientGetRequest request = new();

        _invoiceClientRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceClientList);

        _invoiceClientRepositoryMock.Setup(m => m.GetByUser(It.IsAny<Guid>()))
                        .ReturnsAsync((List<InvoiceClientEntity>)null!);

        List<InvoiceClientModel> expectedResult = _mapper.Map<List<InvoiceClientModel>>(invoiceClientList);

        //Act
        var result = await _invoiceClientService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceClientList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceClientRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceClientRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenClientIdQuery_ReturnsDTO(InvoiceClientGetRequest request, List<InvoiceClientEntity> invoiceClientList)
    {
        //Arrange

        _invoiceClientRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<InvoiceClientEntity>)null!);

        _invoiceClientRepositoryMock.Setup(m => m.GetByUser((Guid)request.UserId!))
                        .ReturnsAsync(invoiceClientList);

        List<InvoiceClientModel> expectedResult = _mapper.Map<List<InvoiceClientModel>>(invoiceClientList);

        //Act
        var result = await _invoiceClientService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceClientList.Count);

        _invoiceClientRepositoryMock.Verify(m => m.Get(), Times.Never());
        _invoiceClientRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        InvoiceClientGetRequest request = new();
        List<InvoiceClientEntity> invoiceClientList = [];

        //Arrange
        _invoiceClientRepositoryMock.Setup(m => m.GetByUser(It.IsAny<Guid>()))
                        .ReturnsAsync(invoiceClientList);

        _invoiceClientRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceClientList);

        // Act Assert
        var result = await _invoiceClientService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<InvoiceClientModel>());

        _invoiceClientRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceClientRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(InvoiceClientModel invoiceClient)
    {
        //Arrange
        InvoiceClientEntity invoiceClientEntity = _mapper.Map<InvoiceClientEntity>(invoiceClient);

        _invoiceClientRepositoryMock.Setup(m => m.Add(It.Is<InvoiceClientEntity>
                                (x => x == invoiceClientEntity)))
                                 .ReturnsAsync(invoiceClient.Id);

        //Act
        Guid result = await _invoiceClientService.Add(invoiceClient);

        //Assert
        result.Should().Be(invoiceClient.Id);

        _invoiceClientRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceClientEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(InvoiceClientModel invoiceClient)
    {
        //Arrange
        InvoiceClientEntity invoiceClientEntity = _mapper.Map<InvoiceClientEntity>(invoiceClient);

        _invoiceClientRepositoryMock.Setup(m => m.Update(It.Is<InvoiceClientEntity>
                                (x => x == invoiceClientEntity)));

        _invoiceClientRepositoryMock.Setup(m => m.Get(invoiceClientEntity.Id))
                                .ReturnsAsync(invoiceClientEntity);

        //Act
        //Assert
        await _invoiceClientService.Invoking(x => x.Update(invoiceClient))
                                        .Should().NotThrowAsync<Exception>();

        _invoiceClientRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _invoiceClientRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceClientEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(InvoiceClientModel invoiceClient)
    {
        //Arrange
        InvoiceClientEntity invoiceClientEntity = _mapper.Map<InvoiceClientEntity>(invoiceClient);

        _invoiceClientRepositoryMock.Setup(m => m.Update(It.Is<InvoiceClientEntity>
                                (x => x == invoiceClientEntity)));

        _invoiceClientRepositoryMock.Setup(m => m.Get(invoiceClient.Id))
                        .ReturnsAsync((InvoiceClientEntity)null!);

        //Act
        //Assert
        await _invoiceClientService.Invoking(x => x.Update(invoiceClient))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceClientRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(InvoiceClientEntity invoiceClient)
    {
        //Arrange
        _invoiceClientRepositoryMock.Setup(m => m.Delete(invoiceClient.Id));

        _invoiceClientRepositoryMock.Setup(m => m.Get(invoiceClient.Id))
                        .ReturnsAsync(invoiceClient);

        //Act
        //Assert
        await _invoiceClientService.Invoking(x => x.Delete(invoiceClient.Id))
                            .Should().NotThrowAsync<Exception>();

        _invoiceClientRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _invoiceClientRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _invoiceClientRepositoryMock.Setup(m => m.Delete(id));

        _invoiceClientRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceClientEntity)null!);

        //Act
        //Assert
        await _invoiceClientService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceClientRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}