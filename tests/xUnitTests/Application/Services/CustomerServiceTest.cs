using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Common;
using Contracts.Requests.Customer;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using System.Net;
using WebAPI.MappingProfiles;

namespace xUnitTests.Application.Services;

public class NumberToWordsLTTest
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly CustomerService _customerService;
    private readonly IMapper _mapper;

    public NumberToWordsLTTest()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>(MockBehavior.Strict);

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new CustomerMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _customerService = new CustomerService(_customerRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(CustomerEntity customer)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = _mapper.Map<CustomerModel>(customer);

        //Act
        CustomerModel result = await _customerService.Get(customer.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _customerService.Get(id));

        _customerRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetIdWithValidation_GivenValidId_ReturnsDTO(CustomerEntity customer)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = _mapper.Map<CustomerModel>(customer);

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

    [Theory]
    [AutoData]
    public async Task GetIdWithValidation_GivenInvalidSellerId_ReturnsErrorModel(CustomerEntity customer)
    {
        // Arrange
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

    [Theory]
    [AutoData]
    public async Task GetIdWithValidation_GivenInvalidId_ReturnsErrorModel(Guid id)
    {
        // Arrange
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

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<CustomerEntity> customerList)
    {
        //Arrange
        CustomerGetRequest request = new();

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(customerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenNullQuery_ReturnsDTO(List<CustomerEntity> customerList)
    {
        //Arrange
        CustomerGetRequest? request = null;

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(customerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(List<CustomerEntity> customerList)
    {
        //Arrange
        CustomerGetRequest? request = new()
        {
            SellerId = new Guid()
        };

        _customerRepositoryMock.Setup(m => m.GetBySellerId((Guid)request.SellerId!))
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(customerList);

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

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(CustomerModel customer)
    {
        //Arrange
        customer.InvoiceNumber = 1;
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.Add(It.Is<CustomerEntity>
                                (x => x == customerEntity)))
                                 .ReturnsAsync(customer.Id);

        //Act
        Guid result = await _customerService.Add(customer);

        //Assert
        result.Should().Be(customer.Id);

        _customerRepositoryMock.Verify(m => m.Add(customerEntity), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

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

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(It.IsAny<CustomerEntity>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task IncreaseInvoiceNumber_ReturnsSuccess(Guid id)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.IncreaseInvoiceNumber(id))
                        .Returns(Task.CompletedTask);

        //Act
        //Assert
        await _customerService.Invoking(x => x.IncreaseInvoiceNumber(id))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.IncreaseInvoiceNumber(id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(CustomerEntity customer)
    {
        //Arrange
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

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
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
