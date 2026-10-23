using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace xUnitTests.Application.Services;

public class InvoiceServiceTest
{
    private readonly Mock<IInvoiceRepository> _invoiceDataRepositoryMock;

    private readonly Mock<IItemService> _itemServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<ISellerService> _sellerServiceMock;
    private readonly Mock<IInvoiceDocumentFactory> _invoiceDocumentFactory;

    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTest()
    {
        _invoiceDataRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        _itemServiceMock = new Mock<IItemService>(MockBehavior.Strict);
        _userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
        _customerServiceMock = new Mock<ICustomerService>(MockBehavior.Strict);
        _sellerServiceMock = new Mock<ISellerService>(MockBehavior.Strict);
        _invoiceDocumentFactory = new Mock<IInvoiceDocumentFactory>(MockBehavior.Strict);

        _invoiceService = new InvoiceService(_userServiceMock.Object, _customerServiceMock.Object,
            _itemServiceMock.Object, _sellerServiceMock.Object, _invoiceDataRepositoryMock.Object, _invoiceDocumentFactory.Object);
    }

    private static InvoiceModel NewInvoiceModel() => new()
    {
        Id = Guid.NewGuid(),
        User = new() { Id = Guid.NewGuid() },
        Seller = new() { Id = Guid.NewGuid() },
        Customer = new() { Id = Guid.NewGuid() },
        Items = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }]
    };

    [Fact]
    public async Task GetId_GivenValidId_ReturnsDTO()
    {
        //Arrange
        InvoiceEntity invoiceData = NewInvoiceModel().ToEntity();

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
                        .ReturnsAsync(invoiceData);

        InvoiceModel expectedResult = invoiceData.ToModel();

        //Act
        InvoiceModel result = await _invoiceService.Get(invoiceData.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _invoiceDataRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((InvoiceEntity)null!);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Get(id))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<InvoiceGetRequest?>> GetNoFilterQueryTestData()
    {
        yield return new((InvoiceGetRequest?)null) { Label = "no query returns all invoices" };
        yield return new(new InvoiceGetRequest()) { Label = "query without filters returns all invoices" };
    }

    [Theory]
    [MemberData(nameof(GetNoFilterQueryTestData))]
    public async Task Get_GivenNoFilter_ReturnsDTO(InvoiceGetRequest? request)
    {
        //Arrange
        List<InvoiceEntity> invoiceDataList = [NewInvoiceModel().ToEntity(), NewInvoiceModel().ToEntity(), NewInvoiceModel().ToEntity()];

        _invoiceDataRepositoryMock.Setup(m => m.Get(null, null, null, 1, (int)PageSize.p25))
                        .ReturnsAsync((invoiceDataList, invoiceDataList.Count));

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

        //Act
        var (result, totalCount) = await _invoiceService.Get(request);

        //Assert
        result.Count().Should().Be(invoiceDataList.Count);
        result.Should().BeEquivalentTo(expectedResult);
        totalCount.Should().Be(invoiceDataList.Count);

        _invoiceDataRepositoryMock.Verify(m => m.Get(null, null, null, 1, (int)PageSize.p25), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<InvoiceGetRequest, Guid?, Guid?, Guid?>> GetFilterQueryTestData()
    {
        Guid userId = Guid.NewGuid();
        Guid sellerId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();

        yield return new(new InvoiceGetRequest { UserId = userId }, userId, null, null) { Label = "user id filter is passed as the user id" };
        yield return new(new InvoiceGetRequest { SellerId = sellerId }, null, sellerId, null) { Label = "seller id filter is passed as the seller id" };
        yield return new(new InvoiceGetRequest { CustomerId = customerId }, null, null, customerId) { Label = "customer id filter is passed as the customer id" };
    }

    [Theory]
    [MemberData(nameof(GetFilterQueryTestData))]
    public async Task Get_GivenFilterQuery_PassesFilterToRepository(InvoiceGetRequest request, Guid? userId, Guid? sellerId, Guid? customerId)
    {
        //Arrange
        List<InvoiceEntity> invoiceDataList = [NewInvoiceModel().ToEntity(), NewInvoiceModel().ToEntity(), NewInvoiceModel().ToEntity()];

        _invoiceDataRepositoryMock.Setup(m => m.Get(userId, sellerId, customerId, request.Page, (int)request.PageSize))
                        .ReturnsAsync((invoiceDataList, invoiceDataList.Count));

        List<InvoiceModel> expectedResult = invoiceDataList.Select(i => i.ToModel()).ToList();

        //Act
        var (result, totalCount) = await _invoiceService.Get(request);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        totalCount.Should().Be(invoiceDataList.Count);

        _invoiceDataRepositoryMock.Verify(m => m.Get(userId, sellerId, customerId, request.Page, (int)request.PageSize), Times.Once());
    }

    [Fact]
    public async Task Add_GivenInValidSeller_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidCustomer_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidItem_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();
        List<Guid> itemIds = invoiceData.Items!.Select(i => i.Id).ToList();

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Customer!);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.Customer!.Id))
            .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.Customer!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenValidData_ReturnsGuid()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();
        invoiceData.Status = InvoiceStatus.Payed;
        List<Guid> itemIds = invoiceData.Items!.Select(i => i.Id).ToList();

        InvoiceEntity invoiceDataEntity = invoiceData.ToEntity() with { Status = InvoiceStatus.Created };
        List<ItemModel> itemModels = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        InvoiceService.MapInvoiceItemToItem(invoiceData.Items!, itemModels);

        _invoiceDataRepositoryMock.Setup(m => m.Add(invoiceDataEntity))
                                 .ReturnsAsync(invoiceDataEntity.Id);

        _userServiceMock.Setup(m => m.Get(invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.User!);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
                .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
                .ReturnsAsync(invoiceData.Customer!);

        _customerServiceMock.Setup(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id))
                .Returns(Task.CompletedTask);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.Customer!.Id))
            .ReturnsAsync(itemModels);

        //Act
        Guid result = await _invoiceService.Add(invoiceData);

        //Assert
        result.Should().Be(invoiceData.Id);

        _userServiceMock.Verify(m => m.Get(invoiceData.User!.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.IncreaseInvoiceNumber(invoiceData.Customer!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.Customer!.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(invoiceDataEntity), Times.Once());
    }

    [Fact]
    public async Task Delete_GivenValidId_DeletesFromRepository()
    {
        //Arrange
        Guid id = Guid.NewGuid();

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

    [Fact]
    public async Task Delete_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

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
    public async Task GeneratePDF_GivenInvalidLanguage_ThrowsValidationException()
    {
        //Arrange
        var invoiceId = Guid.NewGuid();
        var invalidLanguage = (Language)999;
        var validDocumentType = DocumentType.Invoice;

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.GeneratePDF(invoiceId, invalidLanguage, validDocumentType))
            .Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage($"Invalid language code: {invalidLanguage}");
    }

    [Fact]
    public async Task GeneratePDF_GivenInvalidDocumentType_ThrowsValidationException()
    {
        //Arrange
        var invoiceId = Guid.NewGuid();
        var validLanguage = Language.LT;
        var invalidDocumentType = (DocumentType)999;

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.GeneratePDF(invoiceId, validLanguage, invalidDocumentType))
            .Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage($"Invalid document type: {invalidDocumentType}");
    }

    [Fact]
    public async Task GeneratePDF_GivenValidId_ReturnsPdfStream()
    {
        //Arrange
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

        InvoiceModel invoice = invoiceEntity.ToModel();
        var memoryStream = new MemoryStream("dummy pdf content"u8.ToArray());

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceEntity.Id))
                        .ReturnsAsync(invoiceEntity);

        Expression<Func<InvoiceModel, bool>> matchesInvoice = i =>
            i.Id == invoice.Id &&
            i.User == invoice.User &&
            i.CreatedDate == invoice.CreatedDate &&
            i.DueDate == invoice.DueDate &&
            i.Seller == invoice.Seller &&
            i.Customer == invoice.Customer &&
            i.Items != null &&
            i.Items.All(expectedItem => i.Items.Any(invoiceItem => invoiceItem == expectedItem)) &&
            i.InvoiceNumber == invoice.InvoiceNumber &&
            i.Comments == invoice.Comments;

        _invoiceDocumentFactory.Setup(x => x.GeneratePdf(documentType, languageCode, It.Is(matchesInvoice)))
            .Returns(memoryStream);

        //Act
        var (stream, fileName) = await _invoiceService.GeneratePDF(invoiceEntity.Id, languageCode, documentType);

        //Assert
        stream.Should().BeSameAs(memoryStream);
        fileName.Should().Be(invoice.GenerateFileLocation());

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceEntity.Id), Times.Once());
        _invoiceDocumentFactory.Verify(x => x.GeneratePdf(documentType, languageCode, It.Is(matchesInvoice)), Times.Once());
    }

    [Fact]
    public async Task Update_GivenValidInvoice_UpdatesInvoiceAndKeepsStatus()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();
        invoiceData.Status = InvoiceStatus.Payed;
        List<Guid> itemIds = invoiceData.Items!.Select(i => i.Id).ToList();

        var invoiceEntity = invoiceData.ToEntity() with { Status = InvoiceStatus.Send };

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
            .ReturnsAsync(invoiceEntity);

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id))
            .ReturnsAsync(invoiceData.Seller!);

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id))
            .ReturnsAsync(invoiceData.Customer!);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.Customer!.Id))
            .ReturnsAsync(invoiceData.Items!.Select(i => new ItemModel { Id = i.Id, Name = i.Name, Price = i.Price }).ToList());

        _invoiceDataRepositoryMock.Setup(m => m.Update(invoiceEntity))
            .Returns(Task.CompletedTask);

        //Act
        await _invoiceService.Update(invoiceData);

        //Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(invoiceEntity), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Seller!.Id, invoiceData.User!.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.Customer!.Id, invoiceData.Seller!.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.Customer!.Id), Times.Once());
    }

    [Fact]
    public async Task Update_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceData.Id))
            .ReturnsAsync((InvoiceEntity)null!);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceData.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Theory]
    [InlineData(InvoiceStatus.Created, Label = "created status is accepted and saved")]
    [InlineData(InvoiceStatus.Send, Label = "sent status is accepted and saved")]
    [InlineData(InvoiceStatus.Payed, Label = "paid status is accepted and saved")]
    public async Task UpdateStatus_GivenValidStatus_UpdatesStatus(InvoiceStatus validStatus)
    {
        //Arrange
        var invoiceId = Guid.NewGuid();
        var request = new InvoiceUpdateStatusRequest { Id = invoiceId, Status = validStatus };

        var invoiceEntity = new InvoiceEntity { Id = invoiceId, Status = validStatus };
        _invoiceDataRepositoryMock.Setup(m => m.Get(invoiceId)).ReturnsAsync(invoiceEntity);
        _invoiceDataRepositoryMock.Setup(m => m.UpdateStatus(invoiceId, validStatus)).Returns(Task.CompletedTask);

        //Act
        await _invoiceService.UpdateStatus(request);

        //Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceId), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.UpdateStatus(invoiceId, validStatus), Times.Once());
    }

    [Fact]
    public async Task UpdateStatus_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        var request = new InvoiceUpdateStatusRequest { Id = Guid.NewGuid(), Status = InvoiceStatus.Payed };

        _invoiceDataRepositoryMock.Setup(m => m.Get(request.Id))
            .ReturnsAsync((InvoiceEntity)null!);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.UpdateStatus(request))
                            .Should().ThrowAsync<NotFoundException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(request.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.UpdateStatus(It.IsAny<Guid>(), It.IsAny<InvoiceStatus>()), Times.Never());
    }

    [Theory]
    [InlineData(InvoiceStatus.OverDue, "Overdue status cannot be set manually", Label = "overdue is set only by the daily job, so setting it by hand is rejected")]
    [InlineData((InvoiceStatus)99, "Invalid invoice status: 99", Label = "status outside the enum is rejected")]
    public async Task UpdateStatus_GivenInvalidStatus_ThrowsValidationException(InvoiceStatus invalidStatus, string expectedMessage)
    {
        //Arrange
        var invoiceId = Guid.NewGuid();
        var request = new InvoiceUpdateStatusRequest { Id = invoiceId, Status = invalidStatus };

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.UpdateStatus(request))
                            .Should().ThrowAsync<FluentValidation.ValidationException>()
                            .WithMessage(expectedMessage);

        _invoiceDataRepositoryMock.Verify(m => m.Get(invoiceId), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.UpdateStatus(invoiceId, invalidStatus), Times.Never());
    }
}
