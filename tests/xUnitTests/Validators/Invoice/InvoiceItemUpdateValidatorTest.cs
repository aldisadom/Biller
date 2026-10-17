using BillerContracts.Requests.Invoice;
using FluentAssertions;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceItemUpdateValidatorTest
{
    private readonly InvoiceItemUpdateValidator _validator = new();

    private static InvoiceItemUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Item",
        Quantity = 2,
        Price = 9.99m
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
    public void Validate_GivenEmptyId_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.Id = Guid.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Id));
    }

    [Fact]
    public void Validate_GivenEmptyName_IsInvalid()
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

    [Theory]
    [InlineData(0, Label = "zero quantity is rejected")]
    [InlineData(-1, Label = "negative quantity is rejected")]
    [InlineData(-100, Label = "large negative quantity is rejected")]
    public void Validate_GivenQuantityNotGreaterThanZero_IsInvalid(decimal quantity)
    {
        // Arrange
        var request = ValidRequest();
        request.Quantity = quantity;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Quantity));
    }

    [Theory]
    [InlineData(0, Label = "zero price is rejected")]
    [InlineData(-1, Label = "negative price is rejected")]
    [InlineData(-100, Label = "large negative price is rejected")]
    public void Validate_GivenPriceNotGreaterThanZero_IsInvalid(decimal price)
    {
        // Arrange
        var request = ValidRequest();
        request.Price = price;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Price));
    }
}
