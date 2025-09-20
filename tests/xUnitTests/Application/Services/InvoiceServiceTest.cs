using Application.Helpers.PriceToWords;
using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Application.Models.InvoiceGenerationModels;
using Application.Services;
using AutoFixture;
using AutoFixture.Xunit2;
using Common;
using Common.Enums;
using Contracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;

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
    private readonly IFixture _fixture;

    public InvoiceServiceTest()
    {
        _invoiceDataRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        _itemServiceMock = new Mock<IItemService>(MockBehavior.Strict);
        _userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
        _customerServiceMock = new Mock<ICustomerService>(MockBehavior.Strict);
        _sellerServiceMock = new Mock<ISellerService>(MockBehavior.Strict);
        _priceToWords = new Mock<IPriceToWords>(MockBehavior.Strict);
        _invoiceDocumentFactory = new Mock<IInvoiceDocumentFactory>(MockBehavior.Strict);

        _fixture = AutoDataConfigured.CreateFixture();

        _invoiceService = new InvoiceService(_userServiceMock.Object, _customerServiceMock.Object,
            _itemServiceMock.Object, _sellerServiceMock.Object, _invoiceDataRepositoryMock.Object, _invoiceDocumentFactory.Object);
    }

    [Theory]
    [AutoDataConfigured]
    public async Task GetId_GivenValidId_ReturnsDTO(InvoiceEntity invoiceData)
    {
        //Arrange
        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
                        .ReturnsAsync(invoiceData);

        InvoiceModel expectedResult = invoiceData.ToModel();

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

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

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

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

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

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

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

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

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

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

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
    public async Task Add_GivenInValidSeller_Throws(InvoiceModel invoiceData)
    {
        //Arrange
        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity();
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        InvoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is<InvoiceEntity>(x => x == invoiceDataEntity)))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(new ErrorModel());

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Customer!);

        _customerServiceMock.Setup(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id))
                .Returns(Task.CompletedTask);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id))
            .ReturnsAsync(itemModels);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Never());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Add_GivenInValidCustomer_Throws(InvoiceModel invoiceData)
    {
        //Arrange
        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity();
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        InvoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is<InvoiceEntity>(x => x == invoiceDataEntity)))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(new ErrorModel());

        _customerServiceMock.Setup(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id))
                .Returns(Task.CompletedTask);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id))
            .ReturnsAsync(itemModels);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Add_GivenInValidItem_Throws(InvoiceModel invoiceData)
    {
        //Arrange
        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity();
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        InvoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is<InvoiceEntity>(x => x == invoiceDataEntity)))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Customer!);

        _customerServiceMock.Setup(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id))
                .Returns(Task.CompletedTask);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id))
            .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Add_GivenValidData_ReturnsGuid(InvoiceModel invoiceData)
    {
        //Arrange
        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity();
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        InvoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is<InvoiceEntity>(x => x == invoiceDataEntity)))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Customer!);

        _customerServiceMock.Setup(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id))
                .Returns(Task.CompletedTask);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id))
            .ReturnsAsync(itemModels);

        //Act
        Guid result = await _invoiceService.Add(invoiceData);

        //Assert
        result.Should().Be(invoiceData.Id);

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id), Times.Once());
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
    public async Task GeneratePDF_InvalidLanguage_ThrowsValidationException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invalidLanguage = (Language)999;
        var validDocumentType = DocumentType.Invoice;

        // Act
        // Assert
        await _invoiceService.Invoking(x => x.GeneratePDF(invoiceId, invalidLanguage, validDocumentType))
            .Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage($"Invalid language code: {invalidLanguage}");
    }

    [Fact]
    public async Task GeneratePDF_InvalidDocumentType_ThrowsValidationException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var validLanguage = Language.LT;
        var invalidDocumentType = (DocumentType)999;

        // Act
        // Assert
        await _invoiceService.Invoking(x => x.GeneratePDF(invoiceId, validLanguage, invalidDocumentType))
            .Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage($"Invalid document type: {invalidDocumentType}");
    }

    [Fact]
    public async Task GeneratePDF_ValidId_Generates()
    {
        //Arrange
        string invoiceName = "invoice.pdf";
        string invoicePath = Path.Combine(Directory.GetCurrentDirectory(), invoiceName);
        InvoiceEntity invoiceEntity = new()
        {
            Id = Guid.NewGuid(),
            UserData = JsonConvert.SerializeObject(new UserModel()
            {
                Id = Guid.NewGuid(),
                Name = "UserName",
                LastName = "UserLastName",
                Email = "user@email.com",
                Password = "hashedPassword"

            }),
            SellerData = JsonConvert.SerializeObject(new SellerModel()),
            CustomerData = JsonConvert.SerializeObject(new CustomerModel()),
            ItemsData = JsonConvert.SerializeObject(new List<InvoiceItemModel>() { new() }),
            InvoiceNumber = 5,
            Comments = "Test invoice",
            CreatedDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(5)
        };
        Language languageCode = Language.LT;
        DocumentType documentType = DocumentType.Invoice;

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceEntity.Id))
                        .ReturnsAsync(invoiceEntity);

        InvoiceModel invoice = invoiceEntity.ToModel();

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
            i.Comments == invoice.Comments))).Returns(invoiceName);

        invoice.Should().BeEquivalentTo(invoiceEntity.ToModel());
        await File.WriteAllTextAsync(invoiceName, "dummy pdf content");

        try
        {
            // Act        
            FileStream file = await _invoiceService.GeneratePDF(invoiceEntity.Id, languageCode, documentType);
            file.Close();

            // Assert
            _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceEntity.Id), Times.Once());
            Assert.NotNull(file);
            Assert.Equal(invoicePath, file.Name);
        }
        finally
        {
            // Clean up the file after the test
            if (File.Exists(invoicePath))
                File.Delete(invoicePath);
        }
    }

    [Theory]
    [AutoDataConfigured]
    public async Task Update_GivenValidInvoice_UpdatesInvoice(InvoiceModel invoiceData)
    {
        // Arrange
        var invoiceEntity = invoiceData.ToEntity();

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
            .ReturnsAsync(invoiceEntity);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
            .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
            .ReturnsAsync(invoiceData.Customer!);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id))
            .ReturnsAsync(invoiceData.Items!.Select(i => new ItemModel { Id = i.Id, Name = i.Name, Price = i.Price }).ToList());

        _invoiceDataRepositoryMock.Setup(m => m.Update(It.IsAny<InvoiceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.Update(invoiceData);

        // Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceEntity>()), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), invoiceData.Customer!.Id), Times.Once());
    }

    [Fact]
    public async Task UpdateStatus_ValidEnum_UpdatesStatus()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var validStatus = InvoiceStatus.Payed;
        var request = new InvoiceUpdateStatusRequest { Id = invoiceId, Status = validStatus };

        var invoiceEntity = new InvoiceEntity { Id = invoiceId, Status = validStatus };
        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceId)).ReturnsAsync(invoiceEntity);
        _invoiceDataRepositoryMock.Setup(m => m.UpdateStatus(invoiceId, validStatus)).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.UpdateStatus(request);

        // Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceId), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.UpdateStatus(invoiceId, validStatus), Times.Once());
    }

    [Fact]
    public async Task UpdateStatus_InvalidEnum_ThrowsValidationException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invalidStatus = (InvoiceStatus)999;
        var request = new InvoiceUpdateStatusRequest { Id = invoiceId, Status = invalidStatus };

        // Act
        // Assert
        await _invoiceService.Invoking(x => x.UpdateStatus(request))
                            .Should().ThrowAsync<FluentValidation.ValidationException>()
                            .WithMessage($"Invalid invoice status: {invalidStatus}");

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceId), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.UpdateStatus(invoiceId, invalidStatus), Times.Never());
    }
}
