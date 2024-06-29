using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Contracts.Requests.Item;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Services;

public class ItemServiceTest
{
    private readonly Mock<IItemRepository> _itemRepositoryMock;
    private readonly ItemService _itemService;
    private readonly IMapper _mapper;

    public ItemServiceTest()
    {
        _itemRepositoryMock = new Mock<IItemRepository>();

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new ItemMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _itemService = new ItemService(_itemRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync(item);

        ItemModel expectedResult = _mapper.Map<ItemModel>(item);

        //Act
        ItemModel result = await _itemService.Get(item.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((ItemEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.Get(id));

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetIds_GivenValidids_ReturnsDTOs(List<ItemEntity> itemList)
    {
        //Arrange
        List<Guid> ids = itemList.Select(x => x.Id).ToList();

        _itemRepositoryMock.Setup(m => m.Get(ids))
                        .ReturnsAsync(itemList);

        List<ItemModel> expectedResult = _mapper.Map<List<ItemModel>>(itemList);

        //Act
        var result = await _itemService.Get(ids);

        //Assert
        result.Count().Should().Be(itemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(ids), Times.Once());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<ItemEntity> itemList)
    {
        //Arrange
        ItemGetRequest request = new();

        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(itemList);

        _itemRepositoryMock.Setup(m => m.GetByCustomerId(It.IsAny<Guid>()))
                        .ReturnsAsync((List<ItemEntity>)null!);

        List<ItemModel> expectedResult = _mapper.Map<List<ItemModel>>(itemList);

        //Act
        var result = await _itemService.Get(request);

        //Assert
        result.Count().Should().Be(itemList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(ItemGetRequest request, List<ItemEntity> itemList)
    {
        //Arrange

        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<ItemEntity>)null!);

        _itemRepositoryMock.Setup(m => m.GetByCustomerId((Guid)request.CustomerId!))
                        .ReturnsAsync(itemList);

        List<ItemModel> expectedResult = _mapper.Map<List<ItemModel>>(itemList);

        //Act
        var result = await _itemService.Get(request);

        //Assert
        result.Count().Should().Be(itemList.Count);

        _itemRepositoryMock.Verify(m => m.Get(), Times.Never());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        ItemGetRequest request = new();
        List<ItemEntity> itemList = [];

        //Arrange
        _itemRepositoryMock.Setup(m => m.GetByCustomerId(It.IsAny<Guid>()))
                        .ReturnsAsync(itemList);

        _itemRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(itemList);

        // Act Assert
        var result = await _itemService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<ItemModel>());

        _itemRepositoryMock.Verify(m => m.Get(), Times.Once());
        _itemRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(ItemModel item)
    {
        //Arrange
        ItemEntity itemEntity = _mapper.Map<ItemEntity>(item);

        _itemRepositoryMock.Setup(m => m.Add(It.Is<ItemEntity>
                                (x => x == itemEntity)))
                                 .ReturnsAsync(item.Id);

        //Act
        Guid result = await _itemService.Add(item);

        //Assert
        result.Should().Be(item.Id);

        _itemRepositoryMock.Verify(m => m.Add(It.IsAny<ItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(ItemModel item)
    {
        //Arrange
        ItemEntity itemEntity = _mapper.Map<ItemEntity>(item);

        _itemRepositoryMock.Setup(m => m.Update(It.Is<ItemEntity>
                                (x => x == itemEntity)));

        _itemRepositoryMock.Setup(m => m.Get(itemEntity.Id))
                                .ReturnsAsync(itemEntity);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Update(item))
                                        .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _itemRepositoryMock.Verify(m => m.Update(It.IsAny<ItemEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(ItemModel item)
    {
        //Arrange
        ItemEntity itemEntity = _mapper.Map<ItemEntity>(item);

        _itemRepositoryMock.Setup(m => m.Update(It.Is<ItemEntity>
                                (x => x == itemEntity)));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Update(item))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(ItemEntity item)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Delete(item.Id));

        _itemRepositoryMock.Setup(m => m.Get(item.Id))
                        .ReturnsAsync(item);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(item.Id))
                            .Should().NotThrowAsync<Exception>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _itemRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _itemRepositoryMock.Setup(m => m.Delete(id));

        _itemRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((ItemEntity)null!);

        //Act
        //Assert
        await _itemService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _itemRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}
