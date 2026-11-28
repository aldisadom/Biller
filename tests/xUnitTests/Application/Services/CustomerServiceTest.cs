using Application.Interfaces;
using Application.Models;
using Application.Services;
using BillerContracts.Enums;
using BillerContracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using AwesomeAssertions;
using FluentValidation;
using Moq;

namespace xUnitTests.Application.Services;

public class CustomerServiceTest
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ISeriesService> _seriesServiceMock;
    private readonly CustomerService _customerService;

    public CustomerServiceTest()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _seriesServiceMock = new Mock<ISeriesService>(MockBehavior.Strict);

        _customerService = new CustomerService(_customerRepositoryMock.Object, _seriesServiceMock.Object);
    }

    [Fact]
    public async Task GetId_GivenValidId_ReturnsDTO()
    {
        //Arrange
        CustomerEntity customer = new() { Id = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = new() { Id = customer.Id };

        //Act
        CustomerModel result = await _customerService.Get(customer.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Get(id))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task GetForSeller_GivenSellersCustomer_ReturnsCustomer()
    {
        //Arrange
        CustomerEntity customer = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = new() { Id = customer.Id, SellerId = customer.SellerId };

        //Act
        CustomerModel result = await _customerService.GetForSeller(customer.Id, customer.SellerId);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GetForSeller_GivenAnotherSellersCustomer_ThrowsValidationException()
    {
        //Arrange
        CustomerEntity customer = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };
        Guid sellerId = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        //Act
        //Assert
        await _customerService.Invoking(x => x.GetForSeller(customer.Id, sellerId))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage($"Customer id {customer.Id} is invalid for seller id {sellerId}");

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GetForSeller_GivenUnknownCustomer_ThrowsValidationException()
    {
        //Arrange
        Guid id = Guid.NewGuid();
        Guid sellerId = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.GetForSeller(id, sellerId))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage($"Customer id {id} is invalid for seller id {sellerId}");

        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<CustomerGetRequest?, Guid?, int, int>> GetQueryTestData()
    {
        Guid sellerId = Guid.NewGuid();

        yield return new(null, null, 1, 25) { Label = "no query reads the first 25 customers of all sellers" };
        yield return new(new CustomerGetRequest(), null, 1, 25) { Label = "empty query reads the first 25 customers of all sellers" };
        yield return new(new CustomerGetRequest { SellerId = sellerId, Page = 2, PageSize = PageSize.p50 }, sellerId, 2, 50) { Label = "seller filter and paging are passed to the repository" };
    }

    [Theory]
    [MemberData(nameof(GetQueryTestData))]
    public async Task Get_GivenQuery_PassesFilterToRepository(CustomerGetRequest? query, Guid? sellerId, int page, int pageSize)
    {
        //Arrange
        List<CustomerEntity> customerEntities = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

        _customerRepositoryMock.Setup(m => m.Get(sellerId, page, pageSize))
                        .ReturnsAsync((customerEntities, 9, page));

        //Act
        var (customers, totalCount, returnedPage) = await _customerService.Get(query);

        //Assert
        customers.Select(c => c.Id).Should().Equal(customerEntities.Select(c => c.Id));
        totalCount.Should().Be(9);
        returnedPage.Should().Be(page);

        _customerRepositoryMock.Verify(m => m.Get(sellerId, page, pageSize), Times.Once());
    }

    [Fact]
    public async Task Get_GivenPagePastTheEnd_ReturnsPageTheRepositoryReturned()
    {
        //Arrange
        CustomerGetRequest query = new() { Page = 5 };
        List<CustomerEntity> customerEntities = [new() { Id = Guid.NewGuid() }];

        _customerRepositoryMock.Setup(m => m.Get(null, 5, 25))
                        .ReturnsAsync((customerEntities, 4, 1));

        //Act
        var (customers, totalCount, returnedPage) = await _customerService.Get(query);

        //Assert
        customers.Select(c => c.Id).Should().Equal(customerEntities.Select(c => c.Id));
        totalCount.Should().Be(4);
        returnedPage.Should().Be(1);

        _customerRepositoryMock.Verify(m => m.Get(null, 5, 25), Times.Once());
    }

    [Fact]
    public async Task Add_GivenSeriesOfSameSeller_ReturnsGuid()
    {
        //Arrange
        CustomerModel customer = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), SeriesId = Guid.NewGuid() };
        SeriesModel series = new() { Id = customer.SeriesId, SellerId = customer.SellerId };

        CustomerEntity customerEntity = new() { Id = customer.Id, SellerId = customer.SellerId, SeriesId = customer.SeriesId };

        _seriesServiceMock.Setup(m => m.GetForSeller(customer.SeriesId, customer.SellerId))
                            .ReturnsAsync(series);
        _customerRepositoryMock.Setup(m => m.Add(customerEntity))
                                 .ReturnsAsync(customer.Id);

        //Act
        Guid result = await _customerService.Add(customer);

        //Assert
        result.Should().Be(customer.Id);

        _seriesServiceMock.Verify(m => m.GetForSeller(customer.SeriesId, customer.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Add(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Add_GivenSeriesMissingOrOfOtherSeller_ThrowsValidationException()
    {
        //Arrange
        CustomerModel customer = new() { SellerId = Guid.NewGuid(), SeriesId = Guid.NewGuid() };

        _seriesServiceMock.Setup(m => m.GetForSeller(customer.SeriesId, customer.SellerId))
                            .ThrowsAsync(new ValidationException("Series id is invalid"));

        //Act
        //Assert
        await _customerService.Invoking(x => x.Add(customer))
                            .Should().ThrowAsync<ValidationException>().WithMessage("Series id is invalid");

        _seriesServiceMock.Verify(m => m.GetForSeller(customer.SeriesId, customer.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Add(It.IsAny<CustomerEntity>()), Times.Never());
    }

    [Fact]
    public async Task Update_GivenSeriesOfStoredSeller_UpdatesRepository()
    {
        //Arrange
        CustomerEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };
        CustomerModel customer = new() { Id = stored.Id, SeriesId = Guid.NewGuid() };
        SeriesModel series = new() { Id = customer.SeriesId, SellerId = stored.SellerId };

        CustomerEntity customerEntity = new() { Id = customer.Id, SeriesId = customer.SeriesId };

        _customerRepositoryMock.Setup(m => m.Get(stored.Id))
                                .ReturnsAsync(stored);
        _seriesServiceMock.Setup(m => m.GetForSeller(customer.SeriesId, stored.SellerId))
                            .ReturnsAsync(series);
        _customerRepositoryMock.Setup(m => m.Update(customerEntity))
                        .ReturnsAsync(true);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetForSeller(customer.SeriesId, stored.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenNewSeriesWhenCustomerHasInvoices_ThrowsValidationException()
    {
        //Arrange
        CustomerEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };
        CustomerModel customer = new() { Id = stored.Id, SeriesId = Guid.NewGuid() };
        SeriesModel series = new() { Id = customer.SeriesId, SellerId = stored.SellerId };

        CustomerEntity customerEntity = new() { Id = customer.Id, SeriesId = customer.SeriesId };

        _customerRepositoryMock.Setup(m => m.Get(stored.Id))
                                .ReturnsAsync(stored);
        _seriesServiceMock.Setup(m => m.GetForSeller(customer.SeriesId, stored.SellerId))
                            .ReturnsAsync(series);
        _customerRepositoryMock.Setup(m => m.Update(customerEntity))
                        .ReturnsAsync(false);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage($"Customer id {customer.Id} has invoices, so its series can't be changed");

        _customerRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetForSeller(customer.SeriesId, stored.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenCustomerDeletedMeanwhile_ThrowsNotFoundException()
    {
        //Arrange
        CustomerEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };
        CustomerModel customer = new() { Id = stored.Id, SeriesId = Guid.NewGuid() };
        SeriesModel series = new() { Id = customer.SeriesId, SellerId = stored.SellerId };

        CustomerEntity customerEntity = new() { Id = customer.Id, SeriesId = customer.SeriesId };

        _customerRepositoryMock.Setup(m => m.Get(stored.Id))
                                .ReturnsAsync(stored);
        _seriesServiceMock.Setup(m => m.GetForSeller(customer.SeriesId, stored.SellerId))
                            .ReturnsAsync(series);
        _customerRepositoryMock.Setup(m => m.Update(customerEntity))
                        .ReturnsAsync((bool?)null);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<NotFoundException>()
                            .WithMessage($"Invoice customer:{customer.Id} not found");

        _customerRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetForSeller(customer.SeriesId, stored.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenSeriesOfOtherSeller_ThrowsValidationException()
    {
        //Arrange
        CustomerEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };
        CustomerModel customer = new() { Id = stored.Id, SeriesId = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Get(stored.Id))
                                .ReturnsAsync(stored);
        _seriesServiceMock.Setup(m => m.GetForSeller(customer.SeriesId, stored.SellerId))
                            .ThrowsAsync(new ValidationException("Series id is invalid"));

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<ValidationException>().WithMessage("Series id is invalid");

        _customerRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetForSeller(customer.SeriesId, stored.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(It.IsAny<CustomerEntity>()), Times.Never());
    }

    [Fact]
    public async Task Update_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        CustomerModel customer = new() { Id = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetForSeller(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
        _customerRepositoryMock.Verify(m => m.Update(It.IsAny<CustomerEntity>()), Times.Never());
    }

    [Fact]
    public async Task Delete_GivenValidId_DeletesFromRepository()
    {
        //Arrange
        CustomerEntity customer = new() { Id = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Delete(customer.Id))
                        .Returns(Task.CompletedTask);

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Delete(customer.Id))
                            .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
        _customerRepositoryMock.Verify(m => m.Delete(customer.Id), Times.Once());
    }

    [Fact]
    public async Task Delete_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _customerRepositoryMock.Verify(m => m.Delete(id), Times.Never());
    }
}
