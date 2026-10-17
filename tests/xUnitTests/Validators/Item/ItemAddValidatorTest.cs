using BillerContracts.Requests.Item;
using FluentAssertions;
using Validators.Item;

namespace xUnitTests.Validators.Item;

public class ItemAddValidatorTest
{
    private readonly ItemAddValidator _validator = new();

    private static ItemAddRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        Name = "Test Item",
        Price = 9.99m,
        Quantity = 10
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

    public static IEnumerable<TheoryDataRow<Action<ItemAddRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.CustomerId = Guid.Empty, nameof(ItemAddRequest.CustomerId)) { Label = "empty customer id is rejected" };
        yield return new(r => r.Name = string.Empty, nameof(ItemAddRequest.Name)) { Label = "empty name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<ItemAddRequest> clearField, string propertyName)
    {
        // Arrange
        var request = ValidRequest();
        clearField(request);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName);
    }

    [Theory]
    [InlineData(0, Label = "zero price is rejected")]
    [InlineData(-0.01, Label = "price just below zero is rejected")]
    [InlineData(-100, Label = "large negative price is rejected")]
    public void Validate_GivenPriceNotGreaterThanZero_IsInvalid(double price)
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
    public void Validate_GivenZeroQuantity_IsInvalid()
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
    [InlineData(-1, Label = "minus one quantity is rejected")]
    [InlineData(-2, Label = "negative quantity below minus one is rejected")]
    [InlineData(-100, Label = "large negative quantity is rejected")]
    public void Validate_GivenNegativeQuantity_IsInvalid(decimal quantity)
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
    [InlineData(1, Label = "whole quantity is accepted")]
    [InlineData(10, Label = "two digit quantity is accepted")]
    [InlineData(100, Label = "large quantity is accepted")]
    public void Validate_GivenValidQuantity_IsValid(decimal quantity)
    {
        // Arrange
        var request = ValidRequest();
        request.Quantity = quantity;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
