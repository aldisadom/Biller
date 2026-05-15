using Contracts.Requests.Item;
using FluentAssertions;
using Validators.Item;

namespace xUnitTests.Validators.Item;

public class ItemUpdateValidatorTest
{
    private readonly ItemUpdateValidator _validator = new();

    private static ItemUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Item",
        Price = 9.99m,
        Quantity = 10
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
    public void Validate_EmptyId_IsInvalid()
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

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Validate_PriceNotGreaterThanZero_IsInvalid(double price)
    {
        // Arrange
        var request = ValidRequest();
        request.Price = (decimal)price;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Price));
    }

    [Fact]
    public void Validate_ZeroQuantity_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.Quantity = 0;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Quantity));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-100)]
    public void Validate_NegativeQuantity_IsInvalid(decimal quantity)
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
}
