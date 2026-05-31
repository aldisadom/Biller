using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.Item;
using Contracts.Responses;
using Contracts.Responses.Item;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;

namespace xUnitTests.WebAPI.Controllers;

public class ItemControllerTest
{
    private readonly Mock<IItemService> _itemServiceMock;
    private readonly Mock<IValidator<ItemAddRequest>> _validatorAddMock;
    private readonly Mock<IValidator<ItemUpdateRequest>> _validatorUpdateMock;
    private readonly ItemController _itemController;
    private readonly Guid _item1Id = Guid.NewGuid();
    private readonly Guid _item2Id = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    public ItemControllerTest()
    {
        _itemServiceMock = new Mock<IItemService>(MockBehavior.Strict);
        _validatorAddMock = new Mock<IValidator<ItemAddRequest>>(MockBehavior.Strict);
        _validatorUpdateMock = new Mock<IValidator<ItemUpdateRequest>>(MockBehavior.Strict);

        _itemController = new ItemController(
            _itemServiceMock.Object,
            new Mock<ILogger<ItemController>>().Object,
            _validatorAddMock.Object,
            _validatorUpdateMock.Object);
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsOkWithItemResponse()
    {
        // Arrange
        var item = new ItemModel
        {
            Id = _item1Id,
            CustomerId = _customerId
        };

        _itemServiceMock.Setup(s => s.Get(item.Id))
            .ReturnsAsync(item);

        // Act
        var result = await _itemController.Get(item.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(item.ToResponse());

        _itemServiceMock.Verify(s => s.Get(item.Id), Times.Once());
        _itemServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_GivenQuery_ReturnsOkWithItemListResponse()
    {
        // Arrange
        var items = new List<ItemModel>
        {
            new() { Id = _item1Id, CustomerId = _customerId },
            new() { Id = _item2Id, CustomerId = _customerId }
        };
        var query = new ItemGetRequest { CustomerId = _customerId };

        _itemServiceMock.Setup(s => s.Get(query))
            .ReturnsAsync(items);

        // Act
        var result = await _itemController.Get(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new ItemListResponse
            {
                Items = items.Select(i => i.ToResponse()).ToList()
            });

        _itemServiceMock.Verify(s => s.Get(query), Times.Once());
        _itemServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenValidRequest_ReturnsCreatedWithAddResponse()
    {
        // Arrange
        var request = new ItemAddRequest
        {
            CustomerId = _customerId
        };

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _itemServiceMock.Setup(s => s.Add(It.Is<ItemModel>(m => m.CustomerId == _customerId)))
            .ReturnsAsync(_item1Id);

        // Act
        var result = await _itemController.Add(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(new AddResponse { Id = _item1Id });

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _itemServiceMock.Verify(s => s.Add(It.Is<ItemModel>(m => m.CustomerId == _customerId)), Times.Once());
        _itemServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new ItemUpdateRequest
        {
            Id = _item1Id
        };

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _itemServiceMock.Setup(s => s.Update(It.Is<ItemModel>(m => m.Id == _item1Id)))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _itemController.Update(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _itemServiceMock.Verify(s => s.Update(It.Is<ItemModel>(m => m.Id == _item1Id)), Times.Once());
        _itemServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_GivenValidId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _itemServiceMock.Setup(s => s.Delete(id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _itemController.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _itemServiceMock.Verify(s => s.Delete(id), Times.Once());
        _itemServiceMock.VerifyNoOtherCalls();
    }
}
