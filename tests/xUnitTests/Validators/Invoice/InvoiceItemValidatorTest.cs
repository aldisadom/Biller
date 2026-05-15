using Contracts.Requests.Invoice;
using FluentAssertions;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceItemValidatorTest
{
    private readonly InvoiceItemValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        // Arrange
        var request = new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 2 };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_IsInvalid()
    {
        // Arrange
        var request = new InvoiceItemRequest { Id = Guid.Empty, Quantity = 1 };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_QuantityNotGreaterThanZero_IsInvalid(decimal quantity)
    {
        // Arrange
        var request = new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = quantity };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Quantity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0.5)]
    [InlineData(100)]
    public void Validate_ValidQuantity_IsValid(decimal quantity)
    {
        // Arrange
        var request = new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = quantity };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
