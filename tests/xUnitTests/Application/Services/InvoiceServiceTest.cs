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
using AwesomeAssertions;
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
            _itemServiceMock.Object, _sellerServiceMock.Object, _invoiceDataRepositoryMock.Object, _invoiceDocumentFactory.Object,
            RepoFonts.CalibriChecker);
    }

    private static InvoiceModel NewInvoiceModel() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Items = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }]
    };

    [Fact]
    public async Task GetId_GivenValidId_ReturnsStoredInvoiceWithCurrentUserSellerAndCustomer()
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        storedInvoice.SeriesId = Guid.NewGuid();
        storedInvoice.SeriesName = "WF";
        storedInvoice.InvoiceNumber = 5;
        storedInvoice.Items = [new() { Id = Guid.NewGuid(), Name = "Iron throne", Price = 10m, Quantity = 2 }];

        UserModel user = new() { Id = storedInvoice.UserId };
        SellerModel seller = new() { Id = storedInvoice.SellerId };
        CustomerModel customer = new() { Id = storedInvoice.CustomerId };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
                        .ReturnsAsync(storedInvoice.ToEntity());
        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.Get(seller.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.Get(customer.Id))
                .ReturnsAsync(customer);

        //Act
        InvoiceModelFull result = await _invoiceService.Get(storedInvoice.Id);

        //Assert
        result.Id.Should().Be(storedInvoice.Id);
        result.SeriesId.Should().Be(storedInvoice.SeriesId);
        result.SeriesName.Should().Be("WF");
        result.InvoiceNumber.Should().Be(5);
        result.Items.Should().BeEquivalentTo(new List<InvoiceItemModel> { new() { Id = storedInvoice.Items[0].Id, Name = "Iron throne", Price = 10m, Quantity = 2 } });
        result.User.Should().BeSameAs(user);
        result.Seller.Should().BeSameAs(seller);
        result.Customer.Should().BeSameAs(customer);

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(seller.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(customer.Id), Times.Once());
        _sellerServiceMock.VerifyNoOtherCalls();
        _customerServiceMock.VerifyNoOtherCalls();
        _itemServiceMock.VerifyNoOtherCalls();
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
    public async Task Get_GivenInvoicesOfOneCustomer_LoadsUserSellerAndCustomerOnce(InvoiceGetRequest? request)
    {
        //Arrange
        UserModel user = new() { Id = Guid.NewGuid() };
        SellerModel seller = new() { Id = Guid.NewGuid() };
        CustomerModel customer = new() { Id = Guid.NewGuid() };

        InvoiceModel firstInvoice = new() { Id = Guid.NewGuid(), UserId = user.Id, SellerId = seller.Id, CustomerId = customer.Id, InvoiceNumber = 1, Items = [] };
        InvoiceModel secondInvoice = new() { Id = Guid.NewGuid(), UserId = user.Id, SellerId = seller.Id, CustomerId = customer.Id, InvoiceNumber = 2, Items = [] };
        List<InvoiceEntity> invoiceDataList = [firstInvoice.ToEntity(), secondInvoice.ToEntity()];

        _invoiceDataRepositoryMock.Setup(m => m.Get(null, null, null, 1, (int)PageSize.p25))
                        .ReturnsAsync((invoiceDataList, 7, 1));
        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.Get(seller.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.Get(customer.Id))
                .ReturnsAsync(customer);

        //Act
        var (result, totalCount, returnedPage) = await _invoiceService.Get(request);

        //Assert
        result.Select(i => i.Id).Should().Equal(firstInvoice.Id, secondInvoice.Id);
        result.Select(i => i.InvoiceNumber).Should().Equal(1, 2);
        result.Should().AllSatisfy(i =>
        {
            i.User.Should().BeSameAs(user);
            i.Seller.Should().BeSameAs(seller);
            i.Customer.Should().BeSameAs(customer);
        });
        totalCount.Should().Be(7);
        returnedPage.Should().Be(1);

        _invoiceDataRepositoryMock.Verify(m => m.Get(null, null, null, 1, (int)PageSize.p25), Times.Once());
        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(seller.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task Get_GivenInvoicesOfDifferentCustomers_GivesEachInvoiceItsOwnCustomer()
    {
        //Arrange
        UserModel user = new() { Id = Guid.NewGuid() };
        SellerModel seller = new() { Id = Guid.NewGuid() };
        CustomerModel customer = new() { Id = Guid.NewGuid() };
        CustomerModel otherCustomer = new() { Id = Guid.NewGuid() };

        InvoiceModel invoice = new() { Id = Guid.NewGuid(), UserId = user.Id, SellerId = seller.Id, CustomerId = customer.Id, Items = [] };
        InvoiceModel otherInvoice = new() { Id = Guid.NewGuid(), UserId = user.Id, SellerId = seller.Id, CustomerId = otherCustomer.Id, Items = [] };
        List<InvoiceEntity> invoiceDataList = [invoice.ToEntity(), otherInvoice.ToEntity()];
        InvoiceGetRequest request = new() { SellerId = seller.Id };

        _invoiceDataRepositoryMock.Setup(m => m.Get(null, seller.Id, null, request.Page, (int)request.PageSize))
                        .ReturnsAsync((invoiceDataList, invoiceDataList.Count, 1));
        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.Get(seller.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.Get(customer.Id))
                .ReturnsAsync(customer);
        _customerServiceMock.Setup(m => m.Get(otherCustomer.Id))
                .ReturnsAsync(otherCustomer);

        //Act
        var (result, totalCount, returnedPage) = await _invoiceService.Get(request);

        //Assert
        result.Single(i => i.Id == invoice.Id).Customer.Should().BeSameAs(customer);
        result.Single(i => i.Id == otherInvoice.Id).Customer.Should().BeSameAs(otherCustomer);
        totalCount.Should().Be(2);
        returnedPage.Should().Be(1);

        _invoiceDataRepositoryMock.Verify(m => m.Get(null, seller.Id, null, request.Page, (int)request.PageSize), Times.Once());
        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(seller.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(customer.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(otherCustomer.Id), Times.Once());
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
        List<InvoiceEntity> invoiceDataList = [];

        _invoiceDataRepositoryMock.Setup(m => m.Get(userId, sellerId, customerId, request.Page, (int)request.PageSize))
                        .ReturnsAsync((invoiceDataList, 0, 1));

        //Act
        var (result, totalCount, returnedPage) = await _invoiceService.Get(request);

        //Assert
        result.Should().BeEmpty();
        totalCount.Should().Be(0);
        returnedPage.Should().Be(1);

        _invoiceDataRepositoryMock.Verify(m => m.Get(userId, sellerId, customerId, request.Page, (int)request.PageSize), Times.Once());
    }

    [Fact]
    public async Task Add_GivenUnknownUser_ThrowsNotFoundException()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ThrowsAsync(new NotFoundException($"User:{invoiceData.UserId} not found"));

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<NotFoundException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidSeller_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ReturnsAsync(new UserModel { Id = invoiceData.UserId });

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.SellerId, invoiceData.UserId))
                .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.SellerId, invoiceData.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidCustomer_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ReturnsAsync(new UserModel { Id = invoiceData.UserId });

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.SellerId, invoiceData.UserId))
                .ReturnsAsync(new SellerModel { Id = invoiceData.SellerId });

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.CustomerId, invoiceData.SellerId))
                .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.SellerId, invoiceData.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.CustomerId, invoiceData.SellerId), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidItem_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();
        List<Guid> itemIds = invoiceData.Items!.Select(i => i.Id).ToList();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ReturnsAsync(new UserModel { Id = invoiceData.UserId });

        _sellerServiceMock.Setup(m => m.GetWithValidation(invoiceData.SellerId, invoiceData.UserId))
                .ReturnsAsync(new SellerModel { Id = invoiceData.SellerId });

        _customerServiceMock.Setup(m => m.GetWithValidation(invoiceData.CustomerId, invoiceData.SellerId))
                .ReturnsAsync(new CustomerModel { Id = invoiceData.CustomerId });

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.CustomerId))
            .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(invoiceData.SellerId, invoiceData.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(invoiceData.CustomerId, invoiceData.SellerId), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), invoiceData.CustomerId), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenValidData_SavesCustomersSeriesAndCurrentItemNamesAndPrices()
    {
        //Arrange
        Guid throneId = Guid.NewGuid();
        Guid swordId = Guid.NewGuid();
        InvoiceModel invoiceData = new()
        {
            UserId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = InvoiceStatus.Payed,
            CreatedDate = new DateOnly(2026, 10, 1),
            DueDate = new DateOnly(2026, 10, 11),
            Items = [new() { Id = throneId, Quantity = 2 }, new() { Id = swordId, Quantity = 3 }]
        };
        List<Guid> itemIds = [throneId, swordId];

        UserModel user = new() { Id = invoiceData.UserId };
        SellerModel seller = new() { Id = invoiceData.SellerId };
        CustomerModel customer = new() { Id = invoiceData.CustomerId, SeriesId = Guid.NewGuid() };
        List<ItemModel> items =
        [
            new() { Id = throneId, Name = "Iron throne", Price = 10m },
            new() { Id = swordId, Name = "Valyrian sword", Price = 20m }
        ];

        Guid newId = Guid.NewGuid();
        InvoiceEntity expectedEntity = new()
        {
            UserId = invoiceData.UserId,
            SellerId = invoiceData.SellerId,
            CustomerId = invoiceData.CustomerId,
            SeriesId = customer.SeriesId,
            Status = InvoiceStatus.Created,
            CreatedDate = new DateTime(2026, 10, 1),
            DueDate = new DateTime(2026, 10, 11),
            ItemsData = JsonConvert.SerializeObject(new List<InvoiceItemModel>
            {
                new() { Id = throneId, Name = "Iron throne", Price = 10m, Quantity = 2 },
                new() { Id = swordId, Name = "Valyrian sword", Price = 20m, Quantity = 3 }
            }),
            TotalPrice = 80m
        };

        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);

        _sellerServiceMock.Setup(m => m.GetWithValidation(seller.Id, user.Id))
                .ReturnsAsync(seller);

        _customerServiceMock.Setup(m => m.GetWithValidation(customer.Id, seller.Id))
                .ReturnsAsync(customer);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), customer.Id))
            .ReturnsAsync(items);

        _invoiceDataRepositoryMock.Setup(m => m.Add(expectedEntity))
                                 .ReturnsAsync(newId);

        //Act
        Guid result = await _invoiceService.Add(invoiceData);

        //Assert
        result.Should().Be(newId);

        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(seller.Id, user.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(customer.Id, seller.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), customer.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(expectedEntity), Times.Once());
    }

    [Fact]
    public async Task Add_GivenSeriesGoneBeforeInsert_ThrowsValidationException()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();
        List<Guid> itemIds = invoiceData.Items!.Select(i => i.Id).ToList();

        UserModel user = new() { Id = invoiceData.UserId };
        SellerModel seller = new() { Id = invoiceData.SellerId };
        CustomerModel customer = new() { Id = invoiceData.CustomerId, SeriesId = Guid.NewGuid() };
        List<ItemModel> items = invoiceData.Items!.Select(i => new ItemModel() { Id = i.Id }).ToList();

        InvoiceEntity expectedEntity = invoiceData.ToEntity() with { Status = InvoiceStatus.Created, SeriesId = customer.SeriesId };

        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.GetWithValidation(seller.Id, user.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.GetWithValidation(customer.Id, seller.Id))
                .ReturnsAsync(customer);
        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), customer.Id))
            .ReturnsAsync(items);
        _invoiceDataRepositoryMock.Setup(m => m.Add(expectedEntity))
                                 .ReturnsAsync(Guid.Empty);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>()
                            .WithMessage($"Invoice was not created: customer id {customer.Id} is no longer in series id {customer.SeriesId}, or the series has no numbers left");

        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(seller.Id, user.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(customer.Id, seller.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), customer.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(expectedEntity), Times.Once());
    }

    [Fact]
    public async Task Delete_GivenValidId_DeletesFromRepository()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _invoiceDataRepositoryMock.Setup(m => m.Delete(id))
                        .Returns(Task.CompletedTask);

        _invoiceDataRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync(new InvoiceEntity { Id = id });

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

    [Theory]
    [InlineData(DocumentType.Invoice, Language.LT, Label = "LT invoice is named with the invoice's series and number")]
    [InlineData(DocumentType.JobDoneAct, Language.EN, Label = "EN job done act gets the same name as the invoice")]
    public async Task GeneratePDF_GivenValidId_ReturnsPdfNamedWithInvoicesSeriesAndNumber(DocumentType documentType, Language languageCode)
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        storedInvoice.SeriesName = "WF";
        storedInvoice.InvoiceNumber = 5;

        UserModel user = new() { Id = storedInvoice.UserId };
        SellerModel seller = new() { Id = storedInvoice.SellerId };
        CustomerModel customer = new() { Id = storedInvoice.CustomerId, SeriesName = "KL" };

        var memoryStream = new MemoryStream("dummy pdf content"u8.ToArray());

        Expression<Func<InvoiceModelFull, bool>> matchesInvoice = i =>
            i.Id == storedInvoice.Id &&
            i.SeriesName == "WF" &&
            i.InvoiceNumber == 5 &&
            i.User == user &&
            i.Seller == seller &&
            i.Customer == customer;

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
                        .ReturnsAsync(storedInvoice.ToEntity());
        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.Get(seller.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.Get(customer.Id))
                .ReturnsAsync(customer);
        _invoiceDocumentFactory.Setup(x => x.GeneratePdf(documentType, languageCode, It.Is(matchesInvoice)))
            .Returns(memoryStream);

        //Act
        var (stream, fileName) = await _invoiceService.GeneratePDF(storedInvoice.Id, languageCode, documentType);

        //Assert
        stream.Should().BeSameAs(memoryStream);
        fileName.Should().Be("WF-000005.pdf");

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(seller.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(customer.Id), Times.Once());
        _invoiceDocumentFactory.Verify(x => x.GeneratePdf(documentType, languageCode, It.Is(matchesInvoice)), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<Action<InvoiceModel, UserModel, SellerModel, CustomerModel>, string>> GetPrintedTextTestData()
    {
        yield return new((i, u, s, c) => i.SeriesName = "Dragon 🐉", "Series name has characters the invoice PDF can't show: 🐉 (U+1F409). A series with invoices can't be renamed through the API: its name has to be fixed in the database") { Label = "emoji in the series name is named with how to fix it" };
        yield return new((i, u, s, c) => u.Name = "Dragon 🐉", "User name has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the user") { Label = "emoji in the user name is named with how to fix it" };
        yield return new((i, u, s, c) => u.LastName = "Dragon 🐉", "User last name has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the user") { Label = "emoji in the user last name is named with how to fix it" };
        yield return new((i, u, s, c) => s.CompanyName = "Dragon 🐉", "Seller company name has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller company name is named with how to fix it" };
        yield return new((i, u, s, c) => s.CompanyNumber = "Dragon 🐉", "Seller company number has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller company number is named with how to fix it" };
        yield return new((i, u, s, c) => s.Street = "Dragon 🐉", "Seller street has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller street is named with how to fix it" };
        yield return new((i, u, s, c) => s.City = "Dragon 🐉", "Seller city has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller city is named with how to fix it" };
        yield return new((i, u, s, c) => s.Phone = "Dragon 🐉", "Seller phone has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller phone is named with how to fix it" };
        yield return new((i, u, s, c) => s.Email = "Dragon 🐉", "Seller email has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller email is named with how to fix it" };
        yield return new((i, u, s, c) => s.BankName = "Dragon 🐉", "Seller bank name has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller bank name is named with how to fix it" };
        yield return new((i, u, s, c) => s.BankNumber = "Dragon 🐉", "Seller bank number has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the seller") { Label = "emoji in the seller bank number is named with how to fix it" };
        yield return new((i, u, s, c) => c.CompanyName = "Dragon 🐉", "Customer company name has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the customer") { Label = "emoji in the customer company name is named with how to fix it" };
        yield return new((i, u, s, c) => c.CompanyNumber = "Dragon 🐉", "Customer company number has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the customer") { Label = "emoji in the customer company number is named with how to fix it" };
        yield return new((i, u, s, c) => c.Street = "Dragon 🐉", "Customer street has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the customer") { Label = "emoji in the customer street is named with how to fix it" };
        yield return new((i, u, s, c) => c.City = "Dragon 🐉", "Customer city has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the customer") { Label = "emoji in the customer city is named with how to fix it" };
        yield return new((i, u, s, c) => c.Phone = "Dragon 🐉", "Customer phone has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the customer") { Label = "emoji in the customer phone is named with how to fix it" };
        yield return new((i, u, s, c) => c.Email = "Dragon 🐉", "Customer email has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the customer") { Label = "emoji in the customer email is named with how to fix it" };
        yield return new((i, u, s, c) => i.Items![0].Name = "Dragon 🐉", "Item name on line 1 has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the item, then save the invoice again") { Label = "emoji in the item name on the first line is named with how to fix it" };
        yield return new((i, u, s, c) => i.Items![1].Comments = "Dragon 🐉", "Comment on line 2 has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the invoice") { Label = "emoji in the comment on the second line is named with how to fix it" };
        yield return new((i, u, s, c) => i.Comments = "Dragon 🐉", "Invoice comment has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the invoice") { Label = "emoji in the invoice comment is named with how to fix it" };
    }

    [Theory]
    [MemberData(nameof(GetPrintedTextTestData))]
    public async Task GeneratePDF_GivenOnePrintedTextThePdfCannotShow_ThrowsDocumentTextExceptionNamingIt(
        Action<InvoiceModel, UserModel, SellerModel, CustomerModel> setText, string expectedMessage)
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        UserModel user = new() { Id = storedInvoice.UserId };
        SellerModel seller = new() { Id = storedInvoice.SellerId };
        CustomerModel customer = new() { Id = storedInvoice.CustomerId };
        setText(storedInvoice, user, seller, customer);

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
                        .ReturnsAsync(storedInvoice.ToEntity());
        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.Get(seller.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.Get(customer.Id))
                .ReturnsAsync(customer);

        //Act
        Func<Task> act = () => _invoiceService.GeneratePDF(storedInvoice.Id, Language.LT, DocumentType.Invoice);

        //Assert
        // The strict document factory mock has no setup: generating would fail the test
        await act.Should().ThrowAsync<DocumentTextException>()
            .WithMessage(expectedMessage);

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(seller.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GeneratePDF_GivenStoredTextThePdfCannotShow_ThrowsDocumentTextExceptionNamingEveryFieldAndDoesNotGenerate()
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        storedInvoice.Comments = "Fire 🔥";
        storedInvoice.Items = [new() { Name = "Iron throne" }, new() { Name = "Dragon 🐉", Comments = "鐵" }];

        UserModel user = new() { Id = storedInvoice.UserId };
        SellerModel seller = new() { Id = storedInvoice.SellerId, CompanyName = "Lannister gold 🦁" };
        CustomerModel customer = new() { Id = storedInvoice.CustomerId };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
                        .ReturnsAsync(storedInvoice.ToEntity());
        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.Get(seller.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.Get(customer.Id))
                .ReturnsAsync(customer);

        //Act
        Func<Task> act = () => _invoiceService.GeneratePDF(storedInvoice.Id, Language.LT, DocumentType.Invoice);

        //Assert
        // The strict document factory mock has no setup: generating would fail the test
        await act.Should().ThrowAsync<DocumentTextException>()
            .WithMessage("Seller company name has characters the invoice PDF can't show: 🦁 (U+1F981). Edit the seller\n" +
                         "Item name on line 2 has characters the invoice PDF can't show: 🐉 (U+1F409). Edit the item, then save the invoice again\n" +
                         "Comment on line 2 has characters the invoice PDF can't show: 鐵 (U+9435). Edit the invoice\n" +
                         "Invoice comment has characters the invoice PDF can't show: 🔥 (U+1F525). Edit the invoice");

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.Get(seller.Id), Times.Once());
        _customerServiceMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task Update_GivenValidInvoice_KeepsStoredUserStatusSeriesAndNumberAndCopiesCurrentItemPrices()
    {
        //Arrange
        Guid throneId = Guid.NewGuid();
        InvoiceModel storedInvoice = new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesName = "WF",
            InvoiceNumber = 5,
            Status = InvoiceStatus.Send,
            Items = [new() { Id = throneId, Name = "Wooden throne", Price = 8m, Quantity = 1 }]
        };

        InvoiceModel invoiceData = new()
        {
            Id = storedInvoice.Id,
            SellerId = storedInvoice.SellerId,
            CustomerId = storedInvoice.CustomerId,
            SeriesId = Guid.NewGuid(),
            SeriesName = "KL",
            InvoiceNumber = 6,
            Status = InvoiceStatus.Payed,
            Comments = "Winter is coming",
            CreatedDate = new DateOnly(2026, 10, 1),
            DueDate = new DateOnly(2026, 10, 11),
            Items = [new() { Id = throneId, Quantity = 4 }]
        };
        List<Guid> itemIds = [throneId];

        SellerModel seller = new() { Id = invoiceData.SellerId };
        CustomerModel customer = new() { Id = invoiceData.CustomerId };
        List<ItemModel> items = [new() { Id = throneId, Name = "Iron throne", Price = 12m }];

        InvoiceEntity expectedEntity = new()
        {
            Id = storedInvoice.Id,
            UserId = storedInvoice.UserId,
            SellerId = storedInvoice.SellerId,
            CustomerId = storedInvoice.CustomerId,
            SeriesId = storedInvoice.SeriesId,
            SeriesName = "WF",
            InvoiceNumber = 5,
            Status = InvoiceStatus.Send,
            Comments = "Winter is coming",
            CreatedDate = new DateTime(2026, 10, 1),
            DueDate = new DateTime(2026, 10, 11),
            ItemsData = JsonConvert.SerializeObject(new List<InvoiceItemModel>
            {
                new() { Id = throneId, Name = "Iron throne", Price = 12m, Quantity = 4 }
            }),
            TotalPrice = 48m
        };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
            .ReturnsAsync(storedInvoice.ToEntity());

        _sellerServiceMock.Setup(m => m.GetWithValidation(seller.Id, storedInvoice.UserId))
            .ReturnsAsync(seller);

        _customerServiceMock.Setup(m => m.GetWithValidation(customer.Id, seller.Id))
            .ReturnsAsync(customer);

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), customer.Id))
            .ReturnsAsync(items);

        _invoiceDataRepositoryMock.Setup(m => m.Update(expectedEntity))
            .Returns(Task.CompletedTask);

        //Act
        await _invoiceService.Update(invoiceData);

        //Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(seller.Id, storedInvoice.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(customer.Id, seller.Id), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), customer.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(expectedEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenOtherSeller_ThrowsValidationExceptionBeforeValidating()
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        InvoiceModel invoiceData = new()
        {
            Id = storedInvoice.Id,
            SellerId = Guid.NewGuid(),
            CustomerId = storedInvoice.CustomerId,
            Items = storedInvoice.Items
        };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
            .ReturnsAsync(storedInvoice.ToEntity());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>()
                            .WithMessage($"Invoice id {storedInvoice.Id} can't be moved to another seller");

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _customerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Update_GivenOtherCustomer_ThrowsValidationExceptionBeforeValidating()
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        InvoiceModel invoiceData = new()
        {
            Id = storedInvoice.Id,
            SellerId = storedInvoice.SellerId,
            CustomerId = Guid.NewGuid(),
            Items = storedInvoice.Items
        };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
            .ReturnsAsync(storedInvoice.ToEntity());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>()
                            .WithMessage($"Invoice id {storedInvoice.Id} can't be moved to another customer");

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _customerServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Update_GivenItemOfAnotherCustomer_ThrowsAndDoesNotSave()
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        InvoiceModel invoiceData = new()
        {
            Id = storedInvoice.Id,
            SellerId = storedInvoice.SellerId,
            CustomerId = storedInvoice.CustomerId,
            Items = [new() { Id = Guid.NewGuid() }]
        };
        List<Guid> itemIds = [invoiceData.Items[0].Id];

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
            .ReturnsAsync(storedInvoice.ToEntity());

        _sellerServiceMock.Setup(m => m.GetWithValidation(storedInvoice.SellerId, storedInvoice.UserId))
            .ReturnsAsync(new SellerModel { Id = storedInvoice.SellerId });

        _customerServiceMock.Setup(m => m.GetWithValidation(storedInvoice.CustomerId, storedInvoice.SellerId))
            .ReturnsAsync(new CustomerModel { Id = storedInvoice.CustomerId });

        _itemServiceMock.Setup(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), storedInvoice.CustomerId))
            .ReturnsAsync(new ErrorModel());

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>();

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetWithValidation(storedInvoice.SellerId, storedInvoice.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetWithValidation(storedInvoice.CustomerId, storedInvoice.SellerId), Times.Once());
        _itemServiceMock.Verify(m => m.GetWithValidation(It.Is<List<Guid>>(ids => ids.SequenceEqual(itemIds)), storedInvoice.CustomerId), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(It.IsAny<InvoiceEntity>()), Times.Never());
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
    [InlineData(InvoiceStatus.OverDue, "Overdue status cannot be set manually", Label = "overdue cannot be set by hand, so it is rejected")]
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

    [Fact]
    public async Task MarkOverdue_GivenDate_PassesMidnightAndReturnsCount()
    {
        //Arrange
        var today = new DateOnly(2026, 11, 13);
        var midnight = new DateTime(2026, 11, 13, 0, 0, 0);

        _invoiceDataRepositoryMock.Setup(m => m.MarkOverdue(midnight)).ReturnsAsync(3);

        //Act
        int result = await _invoiceService.MarkOverdue(today);

        //Assert
        result.Should().Be(3);
        _invoiceDataRepositoryMock.Verify(m => m.MarkOverdue(midnight), Times.Once());
    }
}
