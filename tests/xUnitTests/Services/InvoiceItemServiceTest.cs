using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.InvoiceItem;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Services;

public class InvoiceItemServiceTest
{
    private readonly Mock<IInvoiceItemRepository> _invoiceItemRepositoryMock;
    private readonly InvoiceItemService _invoiceItemService;
    private readonly IMapper _mapper;

    public InvoiceItemServiceTest()
    {
        _invoiceItemRepositoryMock = new Mock<IInvoiceItemRepository>();

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new InvoiceItemMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _invoiceItemService = new InvoiceItemService(_invoiceItemRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(InvoiceItemEntity invoiceItem)
    {
        //Arrange
        _invoiceItemRepositoryMock.Setup(m => m.Get(invoiceItem.Id))
                        .ReturnsAsync(invoiceItem);

        InvoiceItemModel expectedResult = _mapper.Map<InvoiceItemModel>(invoiceItem);

        //Act
        InvoiceItemModel result = await _invoiceItemService.Get(invoiceItem.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceItemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _invoiceItemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceItemEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _invoiceItemService.Get(id));

        _invoiceItemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetIds_GivenValidids_ReturnsDTOs(List<InvoiceItemEntity> invoiceItemList)
    {
        //Arrange
        List<Guid> ids = invoiceItemList.Select(x => x.Id).ToList();

        _invoiceItemRepositoryMock.Setup(m => m.Get(ids))
                        .ReturnsAsync(invoiceItemList);

        List<InvoiceItemModel> expectedResult = _mapper.Map<List<InvoiceItemModel>>(invoiceItemList);

        //Act
        var result = await _invoiceItemService.Get(ids);

        //Assert
        result.Count().Should().Be(invoiceItemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceItemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
        _invoiceItemRepositoryMock.Verify(m => m.GetByAddressId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<InvoiceItemEntity> invoiceItemList)
    {
        //Arrange
        InvoiceItemGetRequest request = new();

        _invoiceItemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceItemList);

        _invoiceItemRepositoryMock.Setup(m => m.GetByAddressId(It.IsAny<Guid>()))
                        .ReturnsAsync((List<InvoiceItemEntity>)null!);

        List<InvoiceItemModel> expectedResult = _mapper.Map<List<InvoiceItemModel>>(invoiceItemList);

        //Act
        var result = await _invoiceItemService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceItemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceItemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceItemRepositoryMock.Verify(m => m.GetByAddressId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(InvoiceItemGetRequest request, List<InvoiceItemEntity> invoiceItemList)
    {
        //Arrange

        _invoiceItemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<InvoiceItemEntity>)null!);

        _invoiceItemRepositoryMock.Setup(m => m.GetByAddressId((Guid)request.AddressId!))
                        .ReturnsAsync(invoiceItemList);

        List<InvoiceItemModel> expectedResult = _mapper.Map<List<InvoiceItemModel>>(invoiceItemList);

        //Act
        var result = await _invoiceItemService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceItemList.Count);

        _invoiceItemRepositoryMock.Verify(m => m.Get(), Times.Never());
        _invoiceItemRepositoryMock.Verify(m => m.GetByAddressId(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        InvoiceItemGetRequest request = new();
        List<InvoiceItemEntity> invoiceItemList = [];

        //Arrange
        _invoiceItemRepositoryMock.Setup(m => m.GetByAddressId(It.IsAny<Guid>()))
                        .ReturnsAsync(invoiceItemList);

        _invoiceItemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceItemList);

        // Act Assert
        var result = await _invoiceItemService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<InvoiceItemModel>());

        _invoiceItemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceItemRepositoryMock.Verify(m => m.GetByAddressId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(InvoiceItemModel invoiceItem)
    {
        //Arrange
        InvoiceItemEntity invoiceItemEntity = _mapper.Map<InvoiceItemEntity>(invoiceItem);

        _invoiceItemRepositoryMock.Setup(m => m.Add(It.Is<InvoiceItemEntity>
                                (x => x == invoiceItemEntity)))
                                 .ReturnsAsync(invoiceItem.Id);

        //Act
        Guid result = await _invoiceItemService.Add(invoiceItem);

        //Assert
        result.Should().Be(invoiceItem.Id);

        _invoiceItemRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(InvoiceItemModel invoiceItem)
    {
        //Arrange
        InvoiceItemEntity invoiceItemEntity = _mapper.Map<InvoiceItemEntity>(invoiceItem);

        _invoiceItemRepositoryMock.Setup(m => m.Update(It.Is<InvoiceItemEntity>
                                (x => x == invoiceItemEntity)));

        _invoiceItemRepositoryMock.Setup(m => m.Get(invoiceItemEntity.Id))
                                .ReturnsAsync(invoiceItemEntity);

        //Act
        //Assert
        await _invoiceItemService.Invoking(x => x.Update(invoiceItem))
                                        .Should().NotThrowAsync<Exception>();

        _invoiceItemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _invoiceItemRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(InvoiceItemModel invoiceItem)
    {
        //Arrange
        InvoiceItemEntity invoiceItemEntity = _mapper.Map<InvoiceItemEntity>(invoiceItem);

        _invoiceItemRepositoryMock.Setup(m => m.Update(It.Is<InvoiceItemEntity>
                                (x => x == invoiceItemEntity)));

        _invoiceItemRepositoryMock.Setup(m => m.Get(invoiceItem.Id))
                        .ReturnsAsync((InvoiceItemEntity)null!);

        //Act
        //Assert
        await _invoiceItemService.Invoking(x => x.Update(invoiceItem))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceItemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(InvoiceItemEntity invoiceItem)
    {
        //Arrange
        _invoiceItemRepositoryMock.Setup(m => m.Delete(invoiceItem.Id));

        _invoiceItemRepositoryMock.Setup(m => m.Get(invoiceItem.Id))
                        .ReturnsAsync(invoiceItem);

        //Act
        //Assert
        await _invoiceItemService.Invoking(x => x.Delete(invoiceItem.Id))
                            .Should().NotThrowAsync<Exception>();

        _invoiceItemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _invoiceItemRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _invoiceItemRepositoryMock.Setup(m => m.Delete(id));

        _invoiceItemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceItemEntity)null!);

        //Act
        //Assert
        await _invoiceItemService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceItemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}
