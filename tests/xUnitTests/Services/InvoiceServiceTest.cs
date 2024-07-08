﻿using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using WebAPI.MappingProfiles;

namespace xUnitTests.Services;

public class AutoDataConfigured : AutoDataAttribute
{
    public AutoDataConfigured() : base(CreateFixture) { }

    public static IFixture CreateFixture()
    {
        var fixture = new Fixture();

        fixture = new Fixture();
        fixture.Customize<SellerEntity>(c => c
            .With(x => x.Email, "Email" + fixture.Create<string>() + "@gmail.com"));

        fixture.Customize<UserEntity>(c => c
            .With(x => x.Email, "Email" + fixture.Create<string>() + "@gmail.com"));

        fixture.Customize<CustomerEntity>(c => c
            .With(x => x.Email, "Email" + fixture.Create<string>() + "@gmail.com"));

        fixture.Customize<InvoiceDataModel>(c => c
            .With(x=>x.Items,fixture.CreateMany<InvoiceItemModel>(3).ToList())
            .Without(x => x.Customer)
            .Without(x => x.InvoiceNumber)
            .Without(x => x.Items)
            .Do(x =>
            {
                var customer = fixture.Create<CustomerModel>();
                x.Customer = customer;
                x.InvoiceNumber = customer.InvoiceNumber;
            }));
        
        fixture.Customize<InvoiceDataEntity>(c => c
            .Without(x => x.CustomerData)
            .Without(x => x.SellerData)
            .Without(x => x.UserData)
            .Without(x => x.ItemsData)
            .Without(x => x.CustomerId)
            .Without(x => x.SellerId)
            .Without(x => x.UserId)
            .Without(x => x.InvoiceNumber)
            .Do(x =>
            {
                var customer = fixture.Create<CustomerEntity>();
                var seller = fixture.Create<SellerEntity>();
                var user = fixture.Create<UserEntity>();
                var items = fixture.CreateMany<InvoiceItemModel>(3).ToList();
                
                x.CustomerData = JsonConvert.SerializeObject(customer);
                x.SellerData = JsonConvert.SerializeObject(seller);
                x.UserData = JsonConvert.SerializeObject(user);
                x.ItemsData = JsonConvert.SerializeObject(items);
                
                x.CustomerId = customer.Id;
                x.InvoiceNumber = customer.InvoiceNumber;
            }));
        return fixture;
    }
}

public class InvoiceServiceTest
{
    private readonly Mock<IInvoiceRepository> _invoiceDataRepositoryMock;

    private readonly Mock<IItemService> _itemServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<ISellerService> _sellerServiceMock;

    private readonly InvoiceService _invoiceService;
    private readonly IMapper _mapper;
    private readonly IFixture _fixture;

    public InvoiceServiceTest()
    {
        _invoiceDataRepositoryMock = new Mock<IInvoiceRepository>();

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new InvoiceDataMappingProfile());
            mc.AddProfile(new ItemMappingProfile());
            mc.AddProfile(new SellerMappingProfile());
            mc.AddProfile(new CustomerMappingProfile());
            mc.AddProfile(new UserMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _itemServiceMock = new Mock<IItemService>();
        _userServiceMock = new Mock<IUserService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _sellerServiceMock = new Mock<ISellerService>();

        _fixture = AutoDataConfigured.CreateFixture();

        _invoiceService = new InvoiceService(_mapper, _userServiceMock.Object, _customerServiceMock.Object,
            _itemServiceMock.Object, _sellerServiceMock.Object, _invoiceDataRepositoryMock.Object);
    }

    [Theory]
    [AutoDataConfigured]
    public async Task GetId_GivenValidId_ReturnsDTO(InvoiceDataEntity invoiceData)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
                        .ReturnsAsync(invoiceData);

        InvoiceDataModel expectedResult = _mapper.Map<InvoiceDataModel>(invoiceData);

        //Act
        InvoiceDataModel result = await _invoiceService.Get(invoiceData.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceDataEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _invoiceService.Get(id));

        _invoiceDataRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<InvoiceDataEntity> invoiceDataList)
    {
        //Arrange
        InvoiceDataGetRequest request = new();

        _invoiceDataRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceDataModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceDataModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(InvoiceDataGetRequest request, List<InvoiceDataEntity> invoiceDataList)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.GetByCustomerId((Guid)request.CustomerId!))
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceDataModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceDataModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);

        _invoiceDataRepositoryMock.Verify(m => m.GetByCustomerId((Guid)request.CustomerId!), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        InvoiceDataGetRequest request = new();
        List<InvoiceDataEntity> invoiceDataList = [];

        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceDataList);

        // Act Assert
        var result = await _invoiceService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<InvoiceDataModel>());

        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Add_GivenValidId_ReturnsGuid(InvoiceDataModel invoiceData)
    {
        //ArrangeSS
        InvoiceDataEntity invoiceDataEntity = _mapper.Map<InvoiceDataEntity>(invoiceData);
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();
        
        _invoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is<InvoiceDataEntity>
                                (x => x == invoiceDataEntity)))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.Get(invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.Get(invoiceData.Customer!.Id))
                .ReturnsAsync(invoiceData.Customer!);
        /*
        _itemServiceMock.Setup(m => m.Get(invoiceData.Items!.Select(i => i.Id).ToList()))
                .ReturnsAsync(itemModels);
        */
        _itemServiceMock.Setup(m => m.Get(It.IsAny<List<Guid>>()))
            .ReturnsAsync(itemModels);

        //Act
        Guid result = await _invoiceService.Add(invoiceData);

        //Assert
        result.Should().Be(invoiceData.Id);

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(invoiceData.Seller!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(invoiceData.Customer!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.Get(It.IsAny<List<Guid>>()), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceDataEntity>()), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Update_ReturnsSuccess(InvoiceDataModel invoiceData)
    {
        //Arrange
        InvoiceDataEntity invoiceDataEntity = _mapper.Map<InvoiceDataEntity>(invoiceData);

        _invoiceDataRepositoryMock.Setup(m => m.Update(It.Is<InvoiceDataEntity>
                                (x => x == invoiceDataEntity)));

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceDataEntity.Id))
                                .ReturnsAsync(invoiceDataEntity);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                                        .Should().NotThrowAsync<Exception>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(invoiceDataEntity), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Update_InvalidId_NotFoundException(InvoiceDataModel invoiceData)
    {
        //Arrange
        InvoiceDataEntity invoiceDataEntity = _mapper.Map<InvoiceDataEntity>(invoiceData);

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
                        .ReturnsAsync((InvoiceDataEntity)null!);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Delete_ValidId(Guid id)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Delete(id));

        _invoiceDataRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync(new InvoiceDataEntity());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Delete(id))
                            .Should().NotThrowAsync<Exception>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Delete(id), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Delete(id));

        _invoiceDataRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceDataEntity)null!);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }
}
