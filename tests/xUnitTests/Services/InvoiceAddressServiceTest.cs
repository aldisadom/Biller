using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.InvoiceAddress;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Services;

public class InvoiceAddressServiceTest
{
    private readonly Mock<IInvoiceAddressRepository> _invoiceAddressRepositoryMock;
    private readonly InvoiceAddressService _invoiceAddressService;
    private readonly IMapper _mapper;

    public InvoiceAddressServiceTest()
    {
        _invoiceAddressRepositoryMock = new Mock<IInvoiceAddressRepository>();

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new InvoiceAddressMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _invoiceAddressService = new InvoiceAddressService(_invoiceAddressRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(InvoiceAddressEntity invoiceAddress)
    {
        //Arrange
        _invoiceAddressRepositoryMock.Setup(m => m.Get(invoiceAddress.Id))
                        .ReturnsAsync(invoiceAddress);

        InvoiceAddressModel expectedResult = _mapper.Map<InvoiceAddressModel>(invoiceAddress);

        //Act
        InvoiceAddressModel result = await _invoiceAddressService.Get(invoiceAddress.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceAddressRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _invoiceAddressRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceAddressEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _invoiceAddressService.Get(id));

        _invoiceAddressRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<InvoiceAddressEntity> invoiceAddressList)
    {
        //Arrange
        InvoiceAddressGetRequest request = new();

        _invoiceAddressRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceAddressList);

        _invoiceAddressRepositoryMock.Setup(m => m.GetByUser(It.IsAny<Guid>()))
                        .ReturnsAsync((List<InvoiceAddressEntity>)null!);

        List<InvoiceAddressModel> expectedResult = _mapper.Map<List<InvoiceAddressModel>>(invoiceAddressList);

        //Act
        var result = await _invoiceAddressService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceAddressList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceAddressRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceAddressRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(InvoiceAddressGetRequest request, List<InvoiceAddressEntity> invoiceAddressList)
    {
        //Arrange

        _invoiceAddressRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<InvoiceAddressEntity>)null!);

        _invoiceAddressRepositoryMock.Setup(m => m.GetByUser((Guid)request.UserId!))
                        .ReturnsAsync(invoiceAddressList);

        List<InvoiceAddressModel> expectedResult = _mapper.Map<List<InvoiceAddressModel>>(invoiceAddressList);

        //Act
        var result = await _invoiceAddressService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceAddressList.Count);

        _invoiceAddressRepositoryMock.Verify(m => m.Get(), Times.Never());
        _invoiceAddressRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        InvoiceAddressGetRequest request = new();
        List<InvoiceAddressEntity> invoiceAddressList = [];

        //Arrange
        _invoiceAddressRepositoryMock.Setup(m => m.GetByUser(It.IsAny<Guid>()))
                        .ReturnsAsync(invoiceAddressList);

        _invoiceAddressRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceAddressList);

        // Act Assert
        var result = await _invoiceAddressService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<InvoiceAddressModel>());

        _invoiceAddressRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceAddressRepositoryMock.Verify(m => m.GetByUser(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(InvoiceAddressModel invoiceAddress)
    {
        //Arrange
        InvoiceAddressEntity invoiceAddressEntity = _mapper.Map<InvoiceAddressEntity>(invoiceAddress);

        _invoiceAddressRepositoryMock.Setup(m => m.Add(It.Is<InvoiceAddressEntity>
                                (x => x == invoiceAddressEntity)))
                                 .ReturnsAsync(invoiceAddress.Id);

        //Act
        Guid result = await _invoiceAddressService.Add(invoiceAddress);

        //Assert
        result.Should().Be(invoiceAddress.Id);

        _invoiceAddressRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceAddressEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(InvoiceAddressModel invoiceAddress)
    {
        //Arrange
        InvoiceAddressEntity invoiceAddressEntity = _mapper.Map<InvoiceAddressEntity>(invoiceAddress);

        _invoiceAddressRepositoryMock.Setup(m => m.Update(It.Is<InvoiceAddressEntity>
                                (x => x == invoiceAddressEntity)));

        _invoiceAddressRepositoryMock.Setup(m => m.Get(invoiceAddressEntity.Id))
                                .ReturnsAsync(invoiceAddressEntity);

        //Act
        //Assert
        await _invoiceAddressService.Invoking(x => x.Update(invoiceAddress))
                                        .Should().NotThrowAsync<Exception>();

        _invoiceAddressRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _invoiceAddressRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceAddressEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(InvoiceAddressModel invoiceAddress)
    {
        //Arrange
        InvoiceAddressEntity invoiceAddressEntity = _mapper.Map<InvoiceAddressEntity>(invoiceAddress);

        _invoiceAddressRepositoryMock.Setup(m => m.Update(It.Is<InvoiceAddressEntity>
                                (x => x == invoiceAddressEntity)));

        _invoiceAddressRepositoryMock.Setup(m => m.Get(invoiceAddress.Id))
                        .ReturnsAsync((InvoiceAddressEntity)null!);

        //Act
        //Assert
        await _invoiceAddressService.Invoking(x => x.Update(invoiceAddress))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceAddressRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(InvoiceAddressEntity invoiceAddress)
    {
        //Arrange
        _invoiceAddressRepositoryMock.Setup(m => m.Delete(invoiceAddress.Id));

        _invoiceAddressRepositoryMock.Setup(m => m.Get(invoiceAddress.Id))
                        .ReturnsAsync(invoiceAddress);

        //Act
        //Assert
        await _invoiceAddressService.Invoking(x => x.Delete(invoiceAddress.Id))
                            .Should().NotThrowAsync<Exception>();

        _invoiceAddressRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _invoiceAddressRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _invoiceAddressRepositoryMock.Setup(m => m.Delete(id));

        _invoiceAddressRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceAddressEntity)null!);

        //Act
        //Assert
        await _invoiceAddressService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceAddressRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}
