using Application.Interfaces;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Moq;
using System.Net;

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
    public async Task GetIdWithValidation_GivenValidId_ReturnsDTO()
    {
        //Arrange
        CustomerEntity customer = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = new() { Id = customer.Id, SellerId = customer.SellerId };

        //Act
        var resultResponse = await _customerService.GetWithValidation(customer.Id, customer.SellerId);
        CustomerModel result = resultResponse.Match(
            entity => { return entity; },
            error => { throw new Exception(error.ToString()); }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GetIdWithValidation_GivenInvalidSellerId_ReturnsErrorModel()
    {
        //Arrange
        CustomerEntity customer = new() { Id = Guid.NewGuid() };

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        var sellerId = Guid.NewGuid();
        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Customer id {customer.Id} is invalid for seller id {sellerId}"
        };

        //Act
        var resultResponse = await _customerService.GetWithValidation(customer.Id, sellerId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
        error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GetIdWithValidation_GivenInvalidId_ReturnsErrorModel()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        var sellerId = Guid.NewGuid();
        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Customer id {id} is invalid for seller id {sellerId}"
        };

        //Act
        var resultResponse = await _customerService.GetWithValidation(id, sellerId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
        error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<CustomerGetRequest?>> GetNoFilterQueryTestData()
    {
        yield return new((CustomerGetRequest?)null) { Label = "no query returns all customers" };
        yield return new(new CustomerGetRequest()) { Label = "query without filters returns all customers" };
    }

    [Theory]
    [MemberData(nameof(GetNoFilterQueryTestData))]
    public async Task Get_GivenNoFilter_ReturnsDTO(CustomerGetRequest? request)
    {
        //Arrange
        List<CustomerEntity> customerList = [new(), new(), new()];

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = [new(), new(), new()];

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Fact]
    public async Task Get_GivenSellerIdQuery_ReturnsDTO()
    {
        //Arrange
        List<CustomerEntity> customerList = [new(), new(), new()];

        CustomerGetRequest? request = new()
        {
            SellerId = Guid.NewGuid()
        };

        _customerRepositoryMock.Setup(m => m.GetBySellerId((Guid)request.SellerId!))
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = [new(), new(), new()];

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.GetBySellerId((Guid)request.SellerId!), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        //Arrange
        CustomerGetRequest request = new();
        List<CustomerEntity> customerList = [];

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<CustomerModel>());

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Fact]
    public async Task Add_GivenSeriesOfSameSeller_ReturnsGuid()
    {
        //Arrange
        CustomerModel customer = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), SeriesId = Guid.NewGuid() };
        SeriesModel series = new() { Id = customer.SeriesId, SellerId = customer.SellerId };

        CustomerEntity customerEntity = new() { Id = customer.Id, SellerId = customer.SellerId, SeriesId = customer.SeriesId };

        _seriesServiceMock.Setup(m => m.GetWithValidation(customer.SeriesId, customer.SellerId))
                            .ReturnsAsync(series);
        _customerRepositoryMock.Setup(m => m.Add(customerEntity))
                                 .ReturnsAsync(customer.Id);

        //Act
        Guid result = await _customerService.Add(customer);

        //Assert
        result.Should().Be(customer.Id);

        _seriesServiceMock.Verify(m => m.GetWithValidation(customer.SeriesId, customer.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Add(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Add_GivenSeriesMissingOrOfOtherSeller_ThrowsValidationException()
    {
        //Arrange
        CustomerModel customer = new() { SellerId = Guid.NewGuid(), SeriesId = Guid.NewGuid() };
        ErrorModel error = new() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = "Series id is invalid" };

        _seriesServiceMock.Setup(m => m.GetWithValidation(customer.SeriesId, customer.SellerId))
                            .ReturnsAsync(error);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Add(customer))
                            .Should().ThrowAsync<ValidationException>().WithMessage("Series id is invalid");

        _seriesServiceMock.Verify(m => m.GetWithValidation(customer.SeriesId, customer.SellerId), Times.Once());
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
        _seriesServiceMock.Setup(m => m.GetWithValidation(customer.SeriesId, stored.SellerId))
                            .ReturnsAsync(series);
        _customerRepositoryMock.Setup(m => m.Update(customerEntity))
                        .Returns(Task.CompletedTask);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetWithValidation(customer.SeriesId, stored.SellerId), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_GivenSeriesOfOtherSeller_ThrowsValidationException()
    {
        //Arrange
        CustomerEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() };
        CustomerModel customer = new() { Id = stored.Id, SeriesId = Guid.NewGuid() };
        ErrorModel error = new() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = "Series id is invalid" };

        _customerRepositoryMock.Setup(m => m.Get(stored.Id))
                                .ReturnsAsync(stored);
        _seriesServiceMock.Setup(m => m.GetWithValidation(customer.SeriesId, stored.SellerId))
                            .ReturnsAsync(error);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<ValidationException>().WithMessage("Series id is invalid");

        _customerRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesServiceMock.Verify(m => m.GetWithValidation(customer.SeriesId, stored.SellerId), Times.Once());
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
        _seriesServiceMock.Verify(m => m.GetWithValidation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never());
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
