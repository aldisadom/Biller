using BillerContracts.Requests.Customer;
using FluentAssertions;

namespace xUnitTests.Validators.Customer;

public class CustomerUpdateValidatorTest
{
    private readonly CustomerUpdateValidator _validator = new();

    private static CustomerUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        CompanyNumber = "123456",
        CompanyName = "Test Company",
        Street = "Test Street 1",
        City = "Test City",
        State = "Test State",
        Email = "test@example.com",
        Phone = "1234567890",
        InvoiceName = "Test Invoice Name",
        InvoiceNumber = 1
    };

    [Fact]
    public void Validate_GivenValidRequest_IsValid()
    {
        //Arrange
        var request = ValidRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    public static IEnumerable<TheoryDataRow<Action<CustomerUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(CustomerUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.CompanyNumber = string.Empty, nameof(CustomerUpdateRequest.CompanyNumber)) { Label = "empty company number is rejected" };
        yield return new(r => r.CompanyName = string.Empty, nameof(CustomerUpdateRequest.CompanyName)) { Label = "empty company name is rejected" };
        yield return new(r => r.Street = string.Empty, nameof(CustomerUpdateRequest.Street)) { Label = "empty street is rejected" };
        yield return new(r => r.City = string.Empty, nameof(CustomerUpdateRequest.City)) { Label = "empty city is rejected" };
        yield return new(r => r.State = string.Empty, nameof(CustomerUpdateRequest.State)) { Label = "empty state is rejected" };
        yield return new(r => r.Phone = string.Empty, nameof(CustomerUpdateRequest.Phone)) { Label = "empty phone is rejected" };
        yield return new(r => r.InvoiceName = string.Empty, nameof(CustomerUpdateRequest.InvoiceName)) { Label = "empty invoice name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<CustomerUpdateRequest> clearField, string propertyName)
    {
        //Arrange
        var request = ValidRequest();
        clearField(request);

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName);
    }

    [Theory]
    [InlineData("notanemail", Label = "text without an at sign is rejected")]
    [InlineData("@nodomain.com", Label = "address without a local part is rejected")]
    [InlineData("trailing.dot@example.com.", Label = "domain ending with a dot is rejected")]
    [InlineData("", Label = "empty email is rejected")]
    public void Validate_GivenInvalidEmail_IsInvalid(string email)
    {
        //Arrange
        var request = ValidRequest();
        request.Email = email;

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Theory]
    [InlineData(0, Label = "zero invoice number is rejected")]
    [InlineData(-1, Label = "negative invoice number is rejected")]
    [InlineData(-100, Label = "large negative invoice number is rejected")]
    public void Validate_GivenInvoiceNumberNotGreaterThanZero_IsInvalid(int invoiceNumber)
    {
        //Arrange
        var request = ValidRequest();
        request.InvoiceNumber = invoiceNumber;

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InvoiceNumber));
    }
}
