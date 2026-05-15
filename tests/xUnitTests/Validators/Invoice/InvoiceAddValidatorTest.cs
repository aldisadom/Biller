using Contracts.Requests.Invoice;
using FluentAssertions;
using FluentValidation;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceAddValidatorTest
{
    private readonly InvoiceAddValidator _validator = new();

    private static InvoiceAddRequest ValidRequest() => new()
    {
        UserId = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Items =
        [
            new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 2 }
        ],
        CreatedDate = new DateOnly(2024, 1, 1),
        DueDate = new DateOnly(2024, 1, 31)
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
    public void Validate_EmptyUserId_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.UserId = Guid.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.UserId));
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
    public void Validate_EmptyCustomerId_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.CustomerId = Guid.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CustomerId));
    }

    [Fact]
    public void Validate_EmptyItems_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [];

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Items));
    }

    [Fact]
    public void Validate_DueDateBeforeCreatedDate_IsInvalid()
    {
        // Arrange
        var request = ValidRequest();
        request.CreatedDate = new DateOnly(2024, 1, 31);
        request.DueDate = new DateOnly(2024, 1, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.DueDate));
    }

    [Fact]
    public void Validate_DueDateEqualToCreatedDate_IsValid()
    {
        // Arrange
        var request = ValidRequest();
        request.CreatedDate = new DateOnly(2024, 1, 1);
        request.DueDate = new DateOnly(2024, 1, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ItemWithEmptyId_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.Empty, Quantity = 1 }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    [Fact]
    public void Validate_ItemWithZeroQuantity_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 0 }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    [Fact]
    public void Validate_ItemWithNegativeQuantity_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = -1 }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }
}
