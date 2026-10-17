using Application.MappingProfiles;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Requests.Customer;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using System.Net;
using xUnitTests.Helpers;

namespace xUnitTests.Application.Services;

public class CustomerServiceTest
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly CustomerService _customerService;

    public CustomerServiceTest()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>(MockBehavior.Strict);

        _customerService = new CustomerService(_customerRepositoryMock.Object);
    }

    [Fact]
    public async Task GetId_GivenValidId_ReturnsDTO()
    {
        //Arrange
        CustomerEntity customer = TestData.NewCustomerEntity();

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = customer.ToModel();

        //Act
        CustomerModel result = await _customerService.Get(customer.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _customerService.Get(id));

        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task GetIdWithValidation_GivenValidId_ReturnsDTO()
    {
        //Arrange
        CustomerEntity customer = TestData.NewCustomerEntity();

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = customer.ToModel();

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
        // Arrange
        CustomerEntity customer = TestData.NewCustomerEntity();

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        var sellerId = Guid.NewGuid();
        ErrorModel expectedResult = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Message = "Validation failure",
            ExtendedMessage = $"Customer id {customer.Id} is invalid for seller id {sellerId}"
        };

        // Act
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
        // Arrange
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

        // Act
        var resultResponse = await _customerService.GetWithValidation(id, sellerId);
        ErrorModel result = resultResponse.Match(
            entity => { throw new Exception("Got entity not error" + entity.ToString()); },
        error => { return error; }
        );

        //Assert
        result.Should().BeEquivalentTo(expectedResult);
        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmptyQuery_ReturnsDTO()
    {
        //Arrange
        List<CustomerEntity> customerList = [TestData.NewCustomerEntity(), TestData.NewCustomerEntity(), TestData.NewCustomerEntity()];

        CustomerGetRequest request = new();

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = customerList.Select(c => c.ToModel()).ToList();

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Fact]
    public async Task Get_GivenNullQuery_ReturnsDTO()
    {
        //Arrange
        List<CustomerEntity> customerList = [TestData.NewCustomerEntity(), TestData.NewCustomerEntity(), TestData.NewCustomerEntity()];

        CustomerGetRequest? request = null;

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = customerList.Select(c => c.ToModel()).ToList();

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Fact]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO()
    {
        //Arrange
        List<CustomerEntity> customerList = [TestData.NewCustomerEntity(), TestData.NewCustomerEntity(), TestData.NewCustomerEntity()];

        CustomerGetRequest? request = new()
        {
            SellerId = new Guid()
        };

        _customerRepositoryMock.Setup(m => m.GetBySellerId((Guid)request.SellerId!))
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = customerList.Select(c => c.ToModel()).ToList();

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);

        _customerRepositoryMock.Verify(m => m.GetBySellerId((Guid)request.SellerId!), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        CustomerGetRequest request = new();
        List<CustomerEntity> customerList = [];

        //Arrange
        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        // Act Assert
        var result = await _customerService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<CustomerModel>());

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Fact]
    public async Task Add_GivenValidId_ReturnsGuid()
    {
        //Arrange
        CustomerModel customer = TestData.NewCustomerModel();

        customer.InvoiceNumber = 1;
        CustomerEntity customerEntity = customer.ToEntity();

        _customerRepositoryMock.Setup(m => m.Add(It.Is<CustomerEntity>
                                (x => x == customerEntity)))
                                 .ReturnsAsync(customer.Id);

        //Act
        Guid result = await _customerService.Add(customer);

        //Assert
        result.Should().Be(customer.Id);

        _customerRepositoryMock.Verify(m => m.Add(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_ReturnsSuccess()
    {
        //Arrange
        CustomerModel customer = TestData.NewCustomerModel();

        CustomerEntity customerEntity = customer.ToEntity();

        _customerRepositoryMock.Setup(m => m.Update(It.Is<CustomerEntity>(x => x == customerEntity)))
                        .Returns(Task.CompletedTask);

        _customerRepositoryMock.Setup(m => m.Get(customerEntity.Id))
                                .ReturnsAsync(customerEntity);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(customerEntity), Times.Once());
    }

    [Fact]
    public async Task Update_InvalidId_NotFoundException()
    {
        //Arrange
        CustomerModel customer = TestData.NewCustomerModel();

        CustomerEntity customerEntity = customer.ToEntity();

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(It.IsAny<CustomerEntity>()), Times.Never());
    }

    [Fact]
    public async Task IncreaseInvoiceNumber_ReturnsSuccess()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _customerRepositoryMock.Setup(m => m.IncreaseInvoiceNumber(id))
                        .Returns(Task.CompletedTask);

        //Act
        //Assert
        await _customerService.Invoking(x => x.IncreaseInvoiceNumber(id))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.IncreaseInvoiceNumber(id), Times.Once());
    }

    [Fact]
    public async Task Delete_ValidId()
    {
        //Arrange
        CustomerEntity customer = TestData.NewCustomerEntity();

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
    public async Task Delete_InvalidId_ThrowNotFoundException()
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
