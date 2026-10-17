using BillerContracts.Requests.Customer;
using FluentAssertions;
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
    public void Validate_GivenValidRequest_IsValid()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenEmptySellerId_IsInvalid()
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
    public void Validate_GivenEmptyCompanyNumber_IsInvalid()
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
    public void Validate_GivenEmptyCompanyName_IsInvalid()
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
    public void Validate_GivenEmptyStreet_IsInvalid()
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
    public void Validate_GivenEmptyCity_IsInvalid()
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
    public void Validate_GivenEmptyState_IsInvalid()
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
    [InlineData("notanemail", Label = "text without an at sign is rejected")]
    [InlineData("@nodomain.com", Label = "address without a local part is rejected")]
    [InlineData("trailing.dot@example.com.", Label = "domain ending with a dot is rejected")]
    [InlineData("", Label = "empty email is rejected")]
    public void Validate_GivenInvalidEmail_IsInvalid(string email)
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
    public void Validate_GivenEmptyPhone_IsInvalid()
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
    public void Validate_GivenEmptyInvoiceName_IsInvalid()
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
