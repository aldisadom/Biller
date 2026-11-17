using BillerContracts.Requests.Seller;
using AwesomeAssertions;
using Validators.Seller;

namespace xUnitTests.Validators.Seller;

public class SellerUpdateValidatorTest
{
    private readonly SellerUpdateValidator _validator = new(RepoFonts.CalibriChecker);

    private static SellerUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        CompanyNumber = "123456",
        CompanyName = "Test Company",
        Street = "Test Street 1",
        City = "Test City",
        State = "Test State",
        Email = "test@example.com",
        Phone = "1234567890",
        BankName = "Test Bank",
        BankNumber = "LT123456789"
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

    public static IEnumerable<TheoryDataRow<Action<SellerUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(SellerUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.CompanyNumber = string.Empty, nameof(SellerUpdateRequest.CompanyNumber)) { Label = "empty company number is rejected" };
        yield return new(r => r.CompanyName = string.Empty, nameof(SellerUpdateRequest.CompanyName)) { Label = "empty company name is rejected" };
        yield return new(r => r.Street = string.Empty, nameof(SellerUpdateRequest.Street)) { Label = "empty street is rejected" };
        yield return new(r => r.City = string.Empty, nameof(SellerUpdateRequest.City)) { Label = "empty city is rejected" };
        yield return new(r => r.State = string.Empty, nameof(SellerUpdateRequest.State)) { Label = "empty state is rejected" };
        yield return new(r => r.Phone = string.Empty, nameof(SellerUpdateRequest.Phone)) { Label = "empty phone is rejected" };
        yield return new(r => r.BankName = string.Empty, nameof(SellerUpdateRequest.BankName)) { Label = "empty bank name is rejected" };
        yield return new(r => r.BankNumber = string.Empty, nameof(SellerUpdateRequest.BankNumber)) { Label = "empty bank number is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<SellerUpdateRequest> clearField, string propertyName)
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

    public static IEnumerable<TheoryDataRow<Action<SellerUpdateRequest>, string, string>> GetPdfTextFieldTestData()
    {
        yield return new(r => r.CompanyNumber = "Dragon 🐉", nameof(SellerUpdateRequest.CompanyNumber), "Company Number") { Label = "emoji in company number is rejected and named" };
        yield return new(r => r.CompanyName = "Dragon 🐉", nameof(SellerUpdateRequest.CompanyName), "Company Name") { Label = "emoji in company name is rejected and named" };
        yield return new(r => r.Street = "Dragon 🐉", nameof(SellerUpdateRequest.Street), "Street") { Label = "emoji in street is rejected and named" };
        yield return new(r => r.City = "Dragon 🐉", nameof(SellerUpdateRequest.City), "City") { Label = "emoji in city is rejected and named" };
        yield return new(r => r.State = "Dragon 🐉", nameof(SellerUpdateRequest.State), "State") { Label = "emoji in state is rejected and named" };
        yield return new(r => r.Email = "Dragon 🐉", nameof(SellerUpdateRequest.Email), "Email") { Label = "emoji in email is rejected and named" };
        yield return new(r => r.Phone = "Dragon 🐉", nameof(SellerUpdateRequest.Phone), "Phone") { Label = "emoji in phone is rejected and named" };
        yield return new(r => r.BankName = "Dragon 🐉", nameof(SellerUpdateRequest.BankName), "Bank Name") { Label = "emoji in bank name is rejected and named" };
        yield return new(r => r.BankNumber = "Dragon 🐉", nameof(SellerUpdateRequest.BankNumber), "Bank Number") { Label = "emoji in bank number is rejected and named" };
    }

    [Theory]
    [MemberData(nameof(GetPdfTextFieldTestData))]
    public void Validate_GivenCharacterThePdfCannotShow_IsInvalid(Action<SellerUpdateRequest> setField, string propertyName, string displayName)
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
