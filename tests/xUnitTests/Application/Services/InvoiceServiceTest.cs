using Application.Helpers.PriceToWords;
using Application.Interfaces;
using Application.Models;
using Application.Models.InvoiceGenerationModels;
using Application.Services;
using AutoFixture;
using AutoFixture.Xunit2;
using AutoMapper;
using Common.Enums;
using Contracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using WebAPI.MappingProfiles;

namespace xUnitTests.Application.Services;

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

        fixture.Customize<InvoiceModel>(c => c
            .With(x => x.Items, fixture.CreateMany<InvoiceItemModel>(3).ToList())
            .Without(x => x.Customer)
            .Without(x => x.InvoiceNumber)
            .Without(x => x.Items)
            .Without(x => x.CreatedDate)
            .Without(x => x.DueDate)
            .Do(x =>
            {
                var customer = fixture.Create<CustomerModel>();
                x.Customer = customer;
                x.InvoiceNumber = customer.InvoiceNumber;
                x.CreatedDate = DateOnly.FromDateTime(DateTime.Today);
                x.DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
            }));

        fixture.Customize<InvoiceEntity>(c => c
            .Without(x => x.CustomerData)
            .Without(x => x.SellerData)
            .Without(x => x.UserData)
            .Without(x => x.ItemsData)
            .Without(x => x.CustomerId)
            .Without(x => x.SellerId)
            .Without(x => x.UserId)
            .Without(x => x.InvoiceNumber)
            .Without(x => x.CreatedDate)
            .Without(x => x.DueDate)
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
                x.CreatedDate = DateTime.Today;
                x.DueDate = DateTime.Today.AddDays(5);
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
    private readonly Mock<IPriceToWords> _priceToWords;
    private readonly Mock<IInvoiceDocumentFactory> _invoiceDocumentFactory;

    private readonly InvoiceService _invoiceService;
    private readonly IMapper _mapper;
    private readonly IFixture _fixture;

    public InvoiceServiceTest()
    {
        _invoiceDataRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new InvoiceMappingProfile());
            mc.AddProfile(new ItemMappingProfile());
            mc.AddProfile(new SellerMappingProfile());
            mc.AddProfile(new CustomerMappingProfile());
            mc.AddProfile(new UserMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _itemServiceMock = new Mock<IItemService>(MockBehavior.Strict);
        _userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
        _customerServiceMock = new Mock<ICustomerService>(MockBehavior.Strict);
        _sellerServiceMock = new Mock<ISellerService>(MockBehavior.Strict);
        _priceToWords = new Mock<IPriceToWords>(MockBehavior.Strict);
        _invoiceDocumentFactory = new Mock<IInvoiceDocumentFactory>(MockBehavior.Strict);

        _fixture = AutoDataConfigured.CreateFixture();

        _invoiceService = new InvoiceService(_mapper, _userServiceMock.Object, _customerServiceMock.Object,
            _itemServiceMock.Object, _sellerServiceMock.Object, _invoiceDataRepositoryMock.Object, _invoiceDocumentFactory.Object);
    }

    [Theory]
    [AutoDataConfigured]
    public async Task GetId_GivenValidId_ReturnsDTO(InvoiceEntity invoiceData)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
                        .ReturnsAsync(invoiceData);

        InvoiceModel expectedResult = _mapper.Map<InvoiceModel>(invoiceData);

        //Act
        InvoiceModel result = await _invoiceService.Get(invoiceData.Id);

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
                        .ReturnsAsync((InvoiceEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _invoiceService.Get(id));

        _invoiceDataRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<InvoiceEntity> invoiceDataList)
    {
        //Arrange
        InvoiceGetRequest request = new();

        _invoiceDataRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetBySellerId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenNullQuery_ReturnsDTO(List<InvoiceEntity> invoiceDataList)
    {
        //Arrange
        InvoiceGetRequest? request = null;

        _invoiceDataRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetBySellerId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenCustomerIdQuery_ReturnsDTO(List<InvoiceEntity> invoiceDataList)
    {
        //Arrange
        InvoiceGetRequest? request = new()
        {
            CustomerId = new Guid()
        };

        _invoiceDataRepositoryMock.Setup(m => m.GetByCustomerId((Guid)request.CustomerId!))
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);

        _invoiceDataRepositoryMock.Verify(m => m.GetByCustomerId((Guid)request.CustomerId!), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetBySellerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenSellerIdQuery_ReturnsDTO(List<InvoiceEntity> invoiceDataList)
    {
        //Arrange
        InvoiceGetRequest? request = new()
        {
            SellerId = new Guid()
        };

        _invoiceDataRepositoryMock.Setup(m => m.GetBySellerId((Guid)request.SellerId!))
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);

        _invoiceDataRepositoryMock.Verify(m => m.GetBySellerId((Guid)request.SellerId!), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetByUserId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Get_GivenUserIdQuery_ReturnsDTO(List<InvoiceEntity> invoiceDataList)
    {
        //Arrange
        InvoiceGetRequest? request = new()
        {
            UserId = new Guid()
        };

        _invoiceDataRepositoryMock.Setup(m => m.GetByUserId((Guid)request.UserId!))
                        .ReturnsAsync(invoiceDataList);

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => _mapper.Map<InvoiceModel>(i)).ToList();

        //Act
        var result = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);

        _invoiceDataRepositoryMock.Verify(m => m.GetByUserId((Guid)request.UserId!), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Get(), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetByCustomerId(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.GetBySellerId(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Add_GivenValidId_ReturnsGuid(InvoiceModel invoiceData)
    {
        //ArrangeSS
        InvoiceEntity invoiceDataEntity = _mapper.Map<InvoiceEntity>(invoiceData);
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        _invoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is<InvoiceEntity>(x => x == invoiceDataEntity)))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.Get(invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.Get(invoiceData.Customer!.Id))
                .ReturnsAsync(invoiceData.Customer!);

        _customerServiceMock.Setup(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id))
                .Returns(Task.CompletedTask);

        _itemServiceMock.Setup(m => m.Get(It.IsAny<List<Guid>>()))
            .ReturnsAsync(itemModels);

        //Act
        Guid result = await _invoiceService.Add(invoiceData);

        //Assert
        result.Should().Be(invoiceData.Id);

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(invoiceData.Seller!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(invoiceData.Customer!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.Get(It.IsAny<List<Guid>>()), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Once());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Delete_ValidId(Guid id)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Delete(id))
                        .Returns(Task.CompletedTask);

        _invoiceDataRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync(new InvoiceEntity());

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
                        .ReturnsAsync((InvoiceEntity)null!);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task GeneratePDF_ValidId_Generates()
    {
        //Arrange
        InvoiceEntity invoiceEntity = new();
        Language languageCode = Language.LT;
        DocumentType documentType = DocumentType.Invoice;

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceEntity.Id))
                        .ReturnsAsync(invoiceEntity);

        InvoiceModel invoice = _mapper.Map<InvoiceModel>(invoiceEntity);

        _invoiceDocumentFactory.Setup(x => x.GeneratePdf(documentType, languageCode, It.Is<InvoiceModel>(i =>
            i.Id == invoice.Id &&
            i.User == invoice.User &&
            i.CreatedDate == invoice.CreatedDate &&
            i.DueDate == invoice.DueDate &&
            i.Seller == invoice.Seller &&
            i.Customer == invoice.Customer &&
            i.Items != null &&
            i.Items.All(expectedItem => i.Items.Any(invoiceItem => invoiceItem == expectedItem)) &&
            i.InvoiceNumber == invoice.InvoiceNumber &&
            i.Comments == invoice.Comments)));

        //Act        
        await _invoiceService.GeneratePDF(invoiceEntity.Id, languageCode, documentType);

        //Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceEntity.Id), Times.Once());
    }
}
