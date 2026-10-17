using BillerContracts.Requests.Invoice;
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
    public void Validate_GivenValidRequest_IsValid()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    public static IEnumerable<TheoryDataRow<Action<InvoiceAddRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.UserId = Guid.Empty, nameof(InvoiceAddRequest.UserId)) { Label = "empty user id is rejected" };
        yield return new(r => r.SellerId = Guid.Empty, nameof(InvoiceAddRequest.SellerId)) { Label = "empty seller id is rejected" };
        yield return new(r => r.CustomerId = Guid.Empty, nameof(InvoiceAddRequest.CustomerId)) { Label = "empty customer id is rejected" };
        yield return new(r => r.Items = [], nameof(InvoiceAddRequest.Items)) { Label = "empty item list is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<InvoiceAddRequest> clearField, string propertyName)
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

    [Fact]
    public void Validate_GivenDueDateBeforeCreatedDate_IsInvalid()
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
    public void Validate_GivenDueDateEqualToCreatedDate_IsValid()
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
    public void Validate_GivenItemWithEmptyId_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.Empty, Quantity = 1 }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    [Fact]
    public void Validate_GivenItemWithZeroQuantity_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 0 }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    [Fact]
    public void Validate_GivenItemWithNegativeQuantity_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = -1 }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }
}
