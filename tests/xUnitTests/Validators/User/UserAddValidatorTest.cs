using Contracts.Requests.User;
using FluentAssertions;
using Validators.User;

namespace xUnitTests.Validators.User;

public class UserAddValidatorTest
{
    private readonly UserAddValidator _validator = new();

    private static UserAddRequest ValidRequest() => new()
    {
        Name = "John",
        LastName = "Doe",
        Email = "john.doe@example.com",
        Password = "password123"
    };

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.Name = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Name));
    }

    [Fact]
    public void Validate_EmptyLastName_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.LastName = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.LastName));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    [InlineData("trailing.dot@example.com.")]
    [InlineData("")]
    public void Validate_InvalidEmail_IsInvalid(string email)
    {
        // Arrange
        var request = ValidRequest();
        request.Email = email;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("short")]
    public void Validate_WeakPassword_IsInvalid(string password)
    {
        // Arrange
        var request = ValidRequest();
        request.Password = password;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
    }

    [Theory]
    [InlineData("password1")]
    [InlineData("longenoughpassword")]
    [InlineData("12345678")]
    public void Validate_SufficientPassword_IsValid(string password)
    {
        // Arrange
        var request = ValidRequest();
        request.Password = password;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
