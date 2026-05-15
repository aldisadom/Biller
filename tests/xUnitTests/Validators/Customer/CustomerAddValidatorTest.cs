using Contracts.Requests.Customer;
using FluentAssertions;
using FluentValidation;
using Validators.Customer;

namespace xUnitTests.Validators.Customer;

public class CustomerAddValidatorTest
{
    private readonly CustomerAddValidator _validator = new();

    private static CustomerAddRequest ValidRequest() => new()
    {
        SellerId = Guid.NewGuid(),
        CompanyNumber = "123456",
        CompanyName = "Test Company",
        Street = "Test Street 1",
        City = "Test City",
        State = "Test State",
        Email = "test@example.com",
        Phone = "1234567890",
        InvoiceName = "Test Invoice Name"
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
    public void Validate_EmptySellerId_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.SellerId = Guid.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SellerId));
    }

    [Fact]
    public void Validate_EmptyCompanyNumber_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.CompanyNumber = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CompanyNumber));
    }

    [Fact]
    public void Validate_EmptyCompanyName_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.CompanyName = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CompanyName));
    }

    [Fact]
    public void Validate_EmptyStreet_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.Street = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Street));
    }

    [Fact]
    public void Validate_EmptyCity_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.City = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.City));
    }

    [Fact]
    public void Validate_EmptyState_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.State = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.State));
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

    [Fact]
    public void Validate_EmptyPhone_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.Phone = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Phone));
    }

    [Fact]
    public void Validate_EmptyInvoiceName_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.InvoiceName = string.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InvoiceName));
    }
}
