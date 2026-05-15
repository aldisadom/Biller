using Contracts.Requests.Invoice;
using FluentAssertions;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceGenerateValidatorTest
{
    private readonly InvoiceGenerateValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        // Arrange
        var request = new InvoiceGenerateRequest { Id = Guid.NewGuid() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_IsInvalid()
    {
        // Arrange
        var request = new InvoiceGenerateRequest { Id = Guid.Empty };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Id));
    }
}
