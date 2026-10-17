using BillerContracts.Requests.User;
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
    [InlineData("notanemail", Label = "text without an at sign is rejected")]
    [InlineData("@nodomain.com", Label = "address without a local part is rejected")]
    [InlineData("trailing.dot@example.com.", Label = "domain ending with a dot is rejected")]
    [InlineData("", Label = "empty email is rejected")]
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
    [InlineData("", Label = "empty password is rejected")]
    [InlineData("abc", Label = "three letter password is rejected")]
    [InlineData("short", Label = "five letter password is rejected")]
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
    [InlineData("password1", Label = "letters with a digit are accepted")]
    [InlineData("longenoughpassword", Label = "long lowercase password is accepted")]
    [InlineData("12345678", Label = "eight digit password is accepted")]
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
