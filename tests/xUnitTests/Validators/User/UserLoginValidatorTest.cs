using BillerContracts.Requests.User;
using FluentAssertions;
using Validators.User;

namespace xUnitTests.Validators.User;

public class UserLoginValidatorTest
{
    private readonly UserLoginValidator _validator = new();

    [Fact]
    public void Validate_GivenValidRequest_IsValid()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "user@example.com",
            Password = "somepassword"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenEmptyEmail_IsInvalid()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = string.Empty,
            Password = "somepassword"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Fact]
    public void Validate_GivenEmptyPassword_IsInvalid()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "user@example.com",
            Password = string.Empty
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
    }
}
