using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.Seller;
using Contracts.Responses;
using Contracts.Responses.Seller;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;

namespace xUnitTests.WebAPI.Controllers;

public class SellerControllerTest
{
    private readonly Mock<ISellerService> _sellerServiceMock;
    private readonly Mock<IValidator<SellerAddRequest>> _validatorAddMock;
    private readonly Mock<IValidator<SellerUpdateRequest>> _validatorUpdateMock;
    private readonly SellerController _sellerController;
    private readonly Guid _seller1Id = Guid.NewGuid();
    private readonly Guid _seller2Id = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SellerControllerTest()
    {
        _sellerServiceMock = new Mock<ISellerService>(MockBehavior.Strict);
        _validatorAddMock = new Mock<IValidator<SellerAddRequest>>(MockBehavior.Strict);
        _validatorUpdateMock = new Mock<IValidator<SellerUpdateRequest>>(MockBehavior.Strict);

        _sellerController = new SellerController(
            _sellerServiceMock.Object,
            new Mock<ILogger<SellerController>>().Object,
            _validatorAddMock.Object,
            _validatorUpdateMock.Object);
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsOkWithSellerResponse()
    {
        // Arrange
        var seller = new SellerModel
        {
            Id = _seller1Id,
            UserId = _userId
        };

        _sellerServiceMock.Setup(s => s.Get(seller.Id))
            .ReturnsAsync(seller);

        // Act
        var result = await _sellerController.Get(seller.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(seller.ToResponse());

        _sellerServiceMock.Verify(s => s.Get(seller.Id), Times.Once());
        _sellerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_GivenQuery_ReturnsOkWithSellerListResponse()
    {
        // Arrange
        var sellers = new List<SellerModel>
        {
            new() { Id = _seller1Id, UserId = _userId },
            new() { Id = _seller2Id, UserId = _userId }
        };
        var query = new SellerGetRequest { UserId = _userId };

        _sellerServiceMock.Setup(s => s.Get(query))
            .ReturnsAsync(sellers);

        // Act
        var result = await _sellerController.Get(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new SellerListResponse
            {
                Sellers = sellers.Select(s => s.ToResponse()).ToList()
            });

        _sellerServiceMock.Verify(s => s.Get(query), Times.Once());
        _sellerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenValidRequest_ReturnsCreatedWithAddResponse()
    {
        // Arrange
        var request = new SellerAddRequest
        {
            UserId = _userId
        };

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _sellerServiceMock.Setup(s => s.Add(request.ToModel()))
            .ReturnsAsync(_seller1Id);

        // Act
        var result = await _sellerController.Add(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(new AddResponse { Id = _seller1Id });

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _sellerServiceMock.Verify(s => s.Add(request.ToModel()), Times.Once());
        _sellerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new SellerUpdateRequest
        {
            Id = _seller1Id
        };

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _sellerServiceMock.Setup(s => s.Update(request.ToModel()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sellerController.Update(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _sellerServiceMock.Verify(s => s.Update(request.ToModel()), Times.Once());
        _sellerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_GivenValidId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _sellerServiceMock.Setup(s => s.Delete(id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sellerController.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _sellerServiceMock.Verify(s => s.Delete(id), Times.Once());
        _sellerServiceMock.VerifyNoOtherCalls();
    }
}
