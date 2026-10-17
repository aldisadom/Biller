using BillerContracts.Requests.Item;
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
    public void Validate_GivenValidRequest_IsValid()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    public static IEnumerable<TheoryDataRow<Action<ItemUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(ItemUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.Name = string.Empty, nameof(ItemUpdateRequest.Name)) { Label = "empty name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<ItemUpdateRequest> clearField, string propertyName)
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

    [Theory]
    [InlineData(0, Label = "zero quantity is rejected")]
    [InlineData(-1, Label = "minus one quantity is rejected")]
    [InlineData(-2, Label = "negative quantity below minus one is rejected")]
    [InlineData(-100, Label = "large negative quantity is rejected")]
    public void Validate_GivenNonPositiveQuantity_IsInvalid(decimal quantity)
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
