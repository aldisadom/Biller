using BillerContracts.Requests.Customer;
using BillerContracts.Requests.Invoice;
using BillerContracts.Requests.Seller;
using FluentAssertions;
using FluentValidation;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceUpdateValidatorTest
{
    private readonly InvoiceUpdateValidator _validator = new();

    private static SellerUpdateRequest ValidSeller() => new()
    {
        Id = Guid.NewGuid(),
        CompanyNumber = "123456",
        CompanyName = "Seller Company",
        Street = "Seller Street",
        City = "Seller City",
        State = "Seller State",
        Email = "seller@example.com",
        Phone = "1234567890",
        BankName = "Test Bank",
        BankNumber = "LT123456789"
    };

    private static CustomerUpdateRequest ValidCustomer() => new()
    {
        Id = Guid.NewGuid(),
        CompanyNumber = "654321",
        CompanyName = "Customer Company",
        Street = "Customer Street",
        City = "Customer City",
        State = "Customer State",
        Email = "customer@example.com",
        Phone = "0987654321",
        InvoiceName = "Customer Invoice",
        InvoiceNumber = 1
    };

    private static InvoiceUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = 1,
        Seller = ValidSeller(),
        Customer = ValidCustomer(),
        Items =
        [
            new InvoiceItemUpdateRequest { Id = Guid.NewGuid(), Name = "Item 1", Quantity = 2, Price = 10m }
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

    public static IEnumerable<TheoryDataRow<Action<InvoiceUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(InvoiceUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.Items = [], nameof(InvoiceUpdateRequest.Items)) { Label = "empty item list is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<InvoiceUpdateRequest> clearField, string propertyName)
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

    [Theory]
    [InlineData(0, Label = "zero invoice number is rejected")]
    [InlineData(-1, Label = "negative invoice number is rejected")]
    public void Validate_GivenInvoiceNumberNotGreaterThanZero_IsInvalid(int invoiceNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.InvoiceNumber = invoiceNumber;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InvoiceNumber));
    }

    [Fact]
    public void Validate_GivenInvalidSellerEmail_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Seller.Email = "not-an-email";

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    [Fact]
    public void Validate_GivenInvalidCustomerInvoiceNumber_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Customer.InvoiceNumber = 0;

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    [Fact]
    public void Validate_GivenItemWithEmptyName_ThrowsValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemUpdateRequest { Id = Guid.NewGuid(), Name = string.Empty, Quantity = 1, Price = 10m }];

        // Act & Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }
}
