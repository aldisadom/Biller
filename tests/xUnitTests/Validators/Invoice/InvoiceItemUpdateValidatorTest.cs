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

    public static IEnumerable<TheoryDataRow<Action<InvoiceItemUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(InvoiceItemUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.Name = string.Empty, nameof(InvoiceItemUpdateRequest.Name)) { Label = "empty name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<InvoiceItemUpdateRequest> clearField, string propertyName)
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
