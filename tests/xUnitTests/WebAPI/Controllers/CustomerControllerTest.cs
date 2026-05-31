using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.Customer;
using Contracts.Responses;
using Contracts.Responses.Customer;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;

namespace xUnitTests.WebAPI.Controllers;

public class CustomerControllerTest
{
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<IValidator<CustomerAddRequest>> _validatorAddMock;
    private readonly Mock<IValidator<CustomerUpdateRequest>> _validatorUpdateMock;
    private readonly CustomerController _customerController;
    private readonly Guid _customerI1d = Guid.NewGuid();
    private readonly Guid _customerI2d = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();

    public CustomerControllerTest()
    {
        _customerServiceMock = new Mock<ICustomerService>(MockBehavior.Strict);
        _validatorAddMock = new Mock<IValidator<CustomerAddRequest>>(MockBehavior.Strict);
        _validatorUpdateMock = new Mock<IValidator<CustomerUpdateRequest>>(MockBehavior.Strict);

        _customerController = new CustomerController(
            _customerServiceMock.Object,
            new Mock<ILogger<CustomerController>>().Object,
            _validatorAddMock.Object,
            _validatorUpdateMock.Object);
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsOkWithCustomerResponse()
    {
        // Arrange
        var customer = new CustomerModel
        {
            Id = _customerI1d,
            SellerId = _sellerId
        };

        _customerServiceMock.Setup(s => s.Get(customer.Id))
            .ReturnsAsync(customer);

        // Act
        var result = await _customerController.Get(customer.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(customer.ToResponse());

        _customerServiceMock.Verify(s => s.Get(customer.Id), Times.Once());
        _customerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_GivenQuery_ReturnsOkWithCustomerListResponse()
    {
        // Arrange
        var customers = new List<CustomerModel>
        {
            new() { Id = _customerI1d, SellerId = _sellerId },
            new() { Id = _customerI2d, SellerId = _sellerId }
        };
        var query = new CustomerGetRequest { SellerId = _sellerId };

        _customerServiceMock.Setup(s => s.Get(query))
            .ReturnsAsync(customers);

        // Act
        var result = await _customerController.Get(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new CustomerListResponse
            {
                Customers = customers.Select(c => c.ToResponse()).ToList()
            });

        _customerServiceMock.Verify(s => s.Get(query), Times.Once());
        _customerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenValidRequest_ReturnsCreatedWithAddResponse()
    {
        // Arrange
        var request = new CustomerAddRequest
        {
            SellerId = _sellerId
        };

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _customerServiceMock.Setup(s => s.Add(request.ToModel()))
            .ReturnsAsync(_customerI1d);

        // Act
        var result = await _customerController.Add(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(new AddResponse { Id = _customerI1d });

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _customerServiceMock.Verify(s => s.Add(request.ToModel()), Times.Once());
        _customerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new CustomerUpdateRequest
        {
            Id = Guid.NewGuid()
        };

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _customerServiceMock.Setup(s => s.Update(request.ToModel()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _customerController.Update(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _customerServiceMock.Verify(s => s.Update(request.ToModel()), Times.Once());
        _customerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_GivenValidId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _customerServiceMock.Setup(s => s.Delete(id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _customerController.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _customerServiceMock.Verify(s => s.Delete(id), Times.Once());
        _customerServiceMock.VerifyNoOtherCalls();
    }
}
