using BillerContracts.Requests.Customer;
using AwesomeAssertions;

namespace xUnitTests.Validators.Customer;

public class CustomerUpdateValidatorTest
{
    private readonly CustomerUpdateValidator _validator = new(RepoFonts.CalibriChecker);

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
        SeriesId = Guid.NewGuid()
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
        yield return new(r => r.SeriesId = Guid.Empty, nameof(CustomerUpdateRequest.SeriesId)) { Label = "empty series id is rejected" };
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

    public static IEnumerable<TheoryDataRow<Action<CustomerUpdateRequest>, string, string>> GetPdfTextFieldTestData()
    {
        yield return new(r => r.CompanyNumber = "Dragon 🐉", nameof(CustomerUpdateRequest.CompanyNumber), "Company Number") { Label = "emoji in company number is rejected and named" };
        yield return new(r => r.CompanyName = "Dragon 🐉", nameof(CustomerUpdateRequest.CompanyName), "Company Name") { Label = "emoji in company name is rejected and named" };
        yield return new(r => r.Street = "Dragon 🐉", nameof(CustomerUpdateRequest.Street), "Street") { Label = "emoji in street is rejected and named" };
        yield return new(r => r.City = "Dragon 🐉", nameof(CustomerUpdateRequest.City), "City") { Label = "emoji in city is rejected and named" };
        yield return new(r => r.State = "Dragon 🐉", nameof(CustomerUpdateRequest.State), "State") { Label = "emoji in state is rejected and named" };
        yield return new(r => r.Email = "Dragon 🐉", nameof(CustomerUpdateRequest.Email), "Email") { Label = "emoji in email is rejected and named" };
        yield return new(r => r.Phone = "Dragon 🐉", nameof(CustomerUpdateRequest.Phone), "Phone") { Label = "emoji in phone is rejected and named" };
    }

    [Theory]
    [MemberData(nameof(GetPdfTextFieldTestData))]
    public void Validate_GivenCharacterThePdfCannotShow_IsInvalid(Action<CustomerUpdateRequest> setField, string propertyName, string displayName)
    {
        //Arrange
        var request = ValidRequest();
        setField(request);

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName &&
            e.ErrorMessage == $"Please use only characters the invoice PDF can show in {displayName}. Can't show: 🐉 (U+1F409)");
    }

    [Fact]
    public void Validate_GivenLithuanianText_IsValid()
    {
        //Arrange
        var request = ValidRequest();
        request.CompanyNumber = "Ąžuolų šėtra €5";

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }
}
