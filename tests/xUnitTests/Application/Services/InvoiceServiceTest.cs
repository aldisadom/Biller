using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using AwesomeAssertions;
using Moq;
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
        UserId = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Items = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }]
    };

    // InvoiceEntity is a record, but its Items list compares by reference: compare the rest of the
    // entity, then the invoice items one by one.
    private static Expression<Func<InvoiceEntity, bool>> Saved(InvoiceEntity expected) =>
        actual => IsSaved(actual, expected);

    private static bool IsSaved(InvoiceEntity actual, InvoiceEntity expected) =>
        actual with { Items = expected.Items } == expected && actual.Items.SequenceEqual(expected.Items);

    // The item ids ItemService.CheckForCustomer gets, in invoice item order
    private static Expression<Func<List<Guid>, bool>> ItemIds(IEnumerable<Guid> expected) =>
        actual => actual.SequenceEqual(expected);

    [Fact]
    public async Task GetId_GivenValidId_ReturnsStoredInvoiceWithCurrentUserSellerAndCustomer()
    {
        //Arrange
        InvoiceModel storedInvoice = NewInvoiceModel();
        storedInvoice.SeriesId = Guid.NewGuid();
        storedInvoice.SeriesName = "WF";
        storedInvoice.InvoiceNumber = 5;
        storedInvoice.Items = [new() { Id = Guid.NewGuid(), Version = 2, Quantity = 2 }];
        // The repository reads the invoice item's name and price from the item version it names
        InvoiceEntity storedEntity = storedInvoice.ToEntity();
        storedEntity.Items[0].Name = "Iron throne";
        storedEntity.Items[0].Price = 10m;

        UserModel user = new() { Id = storedInvoice.UserId };
        SellerModel seller = new() { Id = storedInvoice.SellerId };
        CustomerModel customer = new() { Id = storedInvoice.CustomerId };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
                        .ReturnsAsync(storedEntity);
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
        result.Items.Should().BeEquivalentTo(new List<InvoiceItemModel> { new() { Id = storedInvoice.Items[0].Id, Version = 2, Name = "Iron throne", Price = 10m, Quantity = 2 } });
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
        var (invoices, totalCount, returnedPage) = await _invoiceService.Get(request);

        //Assert
        invoices.Select(i => i.Id).Should().Equal(firstInvoice.Id, secondInvoice.Id);
        invoices.Select(i => i.InvoiceNumber).Should().Equal(1, 2);
        invoices.Should().AllSatisfy(i =>
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
        var (invoices, totalCount, returnedPage) = await _invoiceService.Get(request);

        //Assert
        invoices.Single(i => i.Id == invoice.Id).Customer.Should().BeSameAs(customer);
        invoices.Single(i => i.Id == otherInvoice.Id).Customer.Should().BeSameAs(otherCustomer);
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
        var (invoices, totalCount, returnedPage) = await _invoiceService.Get(request);

        //Assert
        invoices.Should().BeEmpty();
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
        _sellerServiceMock.Verify(m => m.GetForUser(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidSeller_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ReturnsAsync(new UserModel { Id = invoiceData.UserId });

        _sellerServiceMock.Setup(m => m.GetForUser(invoiceData.SellerId, invoiceData.UserId))
                .ThrowsAsync(new FluentValidation.ValidationException("Ownership check failed"));

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>().WithMessage("Ownership check failed");

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(invoiceData.SellerId, invoiceData.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidCustomer_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ReturnsAsync(new UserModel { Id = invoiceData.UserId });

        _sellerServiceMock.Setup(m => m.GetForUser(invoiceData.SellerId, invoiceData.UserId))
                .ReturnsAsync(new SellerModel { Id = invoiceData.SellerId });

        _customerServiceMock.Setup(m => m.GetForSeller(invoiceData.CustomerId, invoiceData.SellerId))
                .ThrowsAsync(new FluentValidation.ValidationException("Ownership check failed"));

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>().WithMessage("Ownership check failed");

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(invoiceData.SellerId, invoiceData.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(invoiceData.CustomerId, invoiceData.SellerId), Times.Once());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenInValidItem_Throws()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        _userServiceMock.Setup(m => m.Get(invoiceData.UserId))
                .ReturnsAsync(new UserModel { Id = invoiceData.UserId });

        _sellerServiceMock.Setup(m => m.GetForUser(invoiceData.SellerId, invoiceData.UserId))
                .ReturnsAsync(new SellerModel { Id = invoiceData.SellerId });

        _customerServiceMock.Setup(m => m.GetForSeller(invoiceData.CustomerId, invoiceData.SellerId))
                .ReturnsAsync(new CustomerModel { Id = invoiceData.CustomerId });

        _itemServiceMock.Setup(m => m.CheckForCustomer(It.Is(ItemIds(invoiceData.Items!.Select(ii => ii.Id))), invoiceData.CustomerId))
            .ThrowsAsync(new FluentValidation.ValidationException("Ownership check failed"));

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>().WithMessage("Ownership check failed");

        _userServiceMock.Verify(m => m.Get(invoiceData.UserId), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(invoiceData.SellerId, invoiceData.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(invoiceData.CustomerId, invoiceData.SellerId), Times.Once());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.Is(ItemIds(invoiceData.Items!.Select(ii => ii.Id))), invoiceData.CustomerId), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.IsAny<InvoiceEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenValidData_SavesCustomersSeriesAndInvoiceItemVersionsAsGiven()
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
            // The throne has no version: InvoiceRepository.Add gives it the latest. The sword asks for version 1.
            Items = [new() { Id = throneId, Quantity = 2 }, new() { Id = swordId, Version = 1, Quantity = 3 }]
        };
        UserModel user = new() { Id = invoiceData.UserId };
        SellerModel seller = new() { Id = invoiceData.SellerId };
        CustomerModel customer = new() { Id = invoiceData.CustomerId, SeriesId = Guid.NewGuid() };

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
            // The items' names and prices aren't copied: reads join the item version (BIL-85)
            Items =
            [
                new() { LineNo = 1, ItemId = throneId, Version = null, Quantity = 2, Comments = string.Empty },
                new() { LineNo = 2, ItemId = swordId, Version = 1, Quantity = 3, Comments = string.Empty }
            ]
        };

        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);

        _sellerServiceMock.Setup(m => m.GetForUser(seller.Id, user.Id))
                .ReturnsAsync(seller);

        _customerServiceMock.Setup(m => m.GetForSeller(customer.Id, seller.Id))
                .ReturnsAsync(customer);

        _itemServiceMock.Setup(m => m.CheckForCustomer(It.Is(ItemIds(new[] { throneId, swordId })), customer.Id))
            .Returns(Task.CompletedTask);

        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is(Saved(expectedEntity))))
                                 .ReturnsAsync(newId);

        //Act
        Guid result = await _invoiceService.Add(invoiceData);

        //Assert
        result.Should().Be(newId);

        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(seller.Id, user.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(customer.Id, seller.Id), Times.Once());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.Is(ItemIds(new[] { throneId, swordId })), customer.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.Is(Saved(expectedEntity))), Times.Once());
    }

    [Fact]
    public async Task Add_GivenSeriesGoneBeforeInsert_ThrowsValidationException()
    {
        //Arrange
        InvoiceModel invoiceData = NewInvoiceModel();

        UserModel user = new() { Id = invoiceData.UserId };
        SellerModel seller = new() { Id = invoiceData.SellerId };
        CustomerModel customer = new() { Id = invoiceData.CustomerId, SeriesId = Guid.NewGuid() };

        InvoiceEntity expectedEntity = invoiceData.ToEntity() with { Status = InvoiceStatus.Created, SeriesId = customer.SeriesId };

        _userServiceMock.Setup(m => m.Get(user.Id))
                .ReturnsAsync(user);
        _sellerServiceMock.Setup(m => m.GetForUser(seller.Id, user.Id))
                .ReturnsAsync(seller);
        _customerServiceMock.Setup(m => m.GetForSeller(customer.Id, seller.Id))
                .ReturnsAsync(customer);
        _itemServiceMock.Setup(m => m.CheckForCustomer(It.Is(ItemIds(invoiceData.Items!.Select(ii => ii.Id))), customer.Id))
            .Returns(Task.CompletedTask);
        _invoiceDataRepositoryMock.Setup(m => m.Add(It.Is(Saved(expectedEntity))))
                                 .ReturnsAsync(Guid.Empty);

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Add(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>()
                            .WithMessage($"Invoice was not created: customer id {customer.Id} is no longer in series id {customer.SeriesId}, or the series has no numbers left");

        _userServiceMock.Verify(m => m.Get(user.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(seller.Id, user.Id), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(customer.Id, seller.Id), Times.Once());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.Is(ItemIds(invoiceData.Items!.Select(ii => ii.Id))), customer.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Add(It.Is(Saved(expectedEntity))), Times.Once());
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

    [Fact]
    public async Task Update_GivenValidInvoice_KeepsStoredUserStatusSeriesAndNumberAndSavesRequestedVersions()
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
            Items = [new() { Id = throneId, Version = 1, Name = "Wooden throne", Price = 8m, Quantity = 1 }]
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
            // The same item at two versions, while a newer version 3 exists: both are saved as given
            Items = [new() { Id = throneId, Version = 1, Quantity = 4 }, new() { Id = throneId, Version = 2, Quantity = 1 }]
        };
        SellerModel seller = new() { Id = invoiceData.SellerId };
        CustomerModel customer = new() { Id = invoiceData.CustomerId };

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
            Items =
            [
                new() { InvoiceId = storedInvoice.Id, LineNo = 1, ItemId = throneId, Version = 1, Quantity = 4, Comments = string.Empty },
                new() { InvoiceId = storedInvoice.Id, LineNo = 2, ItemId = throneId, Version = 2, Quantity = 1, Comments = string.Empty }
            ]
        };

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
            .ReturnsAsync(storedInvoice.ToEntity());

        _sellerServiceMock.Setup(m => m.GetForUser(seller.Id, storedInvoice.UserId))
            .ReturnsAsync(seller);

        _customerServiceMock.Setup(m => m.GetForSeller(customer.Id, seller.Id))
            .ReturnsAsync(customer);

        _itemServiceMock.Setup(m => m.CheckForCustomer(It.Is(ItemIds(new[] { throneId, throneId })), customer.Id))
            .Returns(Task.CompletedTask);

        _invoiceDataRepositoryMock.Setup(m => m.Update(It.Is(Saved(expectedEntity))))
            .Returns(Task.CompletedTask);

        //Act
        await _invoiceService.Update(invoiceData);

        //Assert
        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(seller.Id, storedInvoice.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(customer.Id, seller.Id), Times.Once());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.Is(ItemIds(new[] { throneId, throneId })), customer.Id), Times.Once());
        _invoiceDataRepositoryMock.Verify(m => m.Update(It.Is(Saved(expectedEntity))), Times.Once());
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
        _sellerServiceMock.Verify(m => m.GetForUser(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _customerServiceMock.Verify(m => m.GetForSeller(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
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
        _sellerServiceMock.Verify(m => m.GetForUser(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _customerServiceMock.Verify(m => m.GetForSeller(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.IsAny<List<Guid>>(), It.IsAny<Guid>()), Times.Never());
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

        _invoiceDataRepositoryMock.Setup(m => m.Get(storedInvoice.Id))
            .ReturnsAsync(storedInvoice.ToEntity());

        _sellerServiceMock.Setup(m => m.GetForUser(storedInvoice.SellerId, storedInvoice.UserId))
            .ReturnsAsync(new SellerModel { Id = storedInvoice.SellerId });

        _customerServiceMock.Setup(m => m.GetForSeller(storedInvoice.CustomerId, storedInvoice.SellerId))
            .ReturnsAsync(new CustomerModel { Id = storedInvoice.CustomerId });

        _itemServiceMock.Setup(m => m.CheckForCustomer(It.Is(ItemIds(invoiceData.Items!.Select(ii => ii.Id))), storedInvoice.CustomerId))
            .ThrowsAsync(new FluentValidation.ValidationException("Ownership check failed"));

        //Act
        //Assert
        await _invoiceService.Invoking(x => x.Update(invoiceData))
                            .Should().ThrowAsync<FluentValidation.ValidationException>().WithMessage("Ownership check failed");

        _invoiceDataRepositoryMock.Verify(m => m.Get(storedInvoice.Id), Times.Once());
        _sellerServiceMock.Verify(m => m.GetForUser(storedInvoice.SellerId, storedInvoice.UserId), Times.Once());
        _customerServiceMock.Verify(m => m.GetForSeller(storedInvoice.CustomerId, storedInvoice.SellerId), Times.Once());
        _itemServiceMock.Verify(m => m.CheckForCustomer(It.Is(ItemIds(invoiceData.Items!.Select(ii => ii.Id))), storedInvoice.CustomerId), Times.Once());
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
