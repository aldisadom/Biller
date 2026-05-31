using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.User;
using Contracts.Responses;
using Contracts.Responses.User;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;

namespace xUnitTests.WebAPI.Controllers;

public class UserControllerTest
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IValidator<UserAddRequest>> _validatorAddMock;
    private readonly Mock<IValidator<UserUpdateRequest>> _validatorUpdateMock;
    private readonly Mock<IValidator<UserLoginRequest>> _validatorLoginMock;
    private readonly UserController _userController;
    private readonly Guid _user1Id = Guid.NewGuid();
    private readonly Guid _user2Id = Guid.NewGuid();

    public UserControllerTest()
    {
        _userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
        _validatorAddMock = new Mock<IValidator<UserAddRequest>>(MockBehavior.Strict);
        _validatorUpdateMock = new Mock<IValidator<UserUpdateRequest>>(MockBehavior.Strict);
        _validatorLoginMock = new Mock<IValidator<UserLoginRequest>>(MockBehavior.Strict);

        _userController = new UserController(
            _userServiceMock.Object,
            new Mock<ILogger<UserController>>().Object,
            _validatorAddMock.Object,
            _validatorUpdateMock.Object,
            _validatorLoginMock.Object);
    }

    [Fact]
    public async Task Login_GivenValidRequest_ReturnsOkWithToken()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "test@test.com",
            Password = "password"
        };
        var token = "token";

        _validatorLoginMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _userServiceMock.Setup(s => s.Login(request.ToModel()))
            .ReturnsAsync(token);

        // Act
        var result = await _userController.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new UserLoginResponse { Token = token });

        _validatorLoginMock.Verify(v => v.Validate(request), Times.Once());
        _validatorLoginMock.VerifyNoOtherCalls();
        _userServiceMock.Verify(s => s.Login(request.ToModel()), Times.Once());
        _userServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsOkWithUserResponse()
    {
        // Arrange
        var user = new UserModel
        {
            Id = _user1Id
        };

        _userServiceMock.Setup(s => s.Get(user.Id))
            .ReturnsAsync(user);

        // Act
        var result = await _userController.Get(user.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(user.ToResponse());

        _userServiceMock.Verify(s => s.Get(user.Id), Times.Once());
        _userServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ReturnsOkWithUserListResponse()
    {
        // Arrange
        var users = new List<UserModel>
        {
            new() { Id = _user1Id },
            new() { Id = _user2Id }
        };

        _userServiceMock.Setup(s => s.Get())
            .ReturnsAsync(users);

        // Act
        var result = await _userController.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new UserListResponse
            {
                Users = users.Select(u => u.ToResponse()).ToList()
            });

        _userServiceMock.Verify(s => s.Get(), Times.Once());
        _userServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenValidRequest_ReturnsCreatedWithAddResponse()
    {
        // Arrange
        var request = new UserAddRequest
        {
            Email = "test@test.com"
        };

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _userServiceMock.Setup(s => s.Add(request.ToModel()))
            .ReturnsAsync(_user1Id);

        // Act
        var result = await _userController.Add(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(new AddResponse { Id = _user1Id });

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _userServiceMock.Verify(s => s.Add(request.ToModel()), Times.Once());
        _userServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new UserUpdateRequest
        {
            Id = _user1Id
        };

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _userServiceMock.Setup(s => s.Update(request.ToModel()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.Update(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _userServiceMock.Verify(s => s.Update(request.ToModel()), Times.Once());
        _userServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_GivenValidId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _userServiceMock.Setup(s => s.Delete(id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _userServiceMock.Verify(s => s.Delete(id), Times.Once());
        _userServiceMock.VerifyNoOtherCalls();
    }
}
