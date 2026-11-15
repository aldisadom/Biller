using BillerContracts.Requests.Seller;
using AwesomeAssertions;
using Validators.Seller;

namespace xUnitTests.Validators.Seller;

public class SellerAddValidatorTest
{
    private readonly SellerAddValidator _validator = new(RepoFonts.CalibriChecker);

    private static SellerAddRequest ValidRequest() => new()
    {
        UserId = Guid.NewGuid(),
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

    public static IEnumerable<TheoryDataRow<Action<SellerAddRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.UserId = Guid.Empty, nameof(SellerAddRequest.UserId)) { Label = "empty user id is rejected" };
        yield return new(r => r.CompanyNumber = string.Empty, nameof(SellerAddRequest.CompanyNumber)) { Label = "empty company number is rejected" };
        yield return new(r => r.CompanyName = string.Empty, nameof(SellerAddRequest.CompanyName)) { Label = "empty company name is rejected" };
        yield return new(r => r.Street = string.Empty, nameof(SellerAddRequest.Street)) { Label = "empty street is rejected" };
        yield return new(r => r.City = string.Empty, nameof(SellerAddRequest.City)) { Label = "empty city is rejected" };
        yield return new(r => r.State = string.Empty, nameof(SellerAddRequest.State)) { Label = "empty state is rejected" };
        yield return new(r => r.Phone = string.Empty, nameof(SellerAddRequest.Phone)) { Label = "empty phone is rejected" };
        yield return new(r => r.BankName = string.Empty, nameof(SellerAddRequest.BankName)) { Label = "empty bank name is rejected" };
        yield return new(r => r.BankNumber = string.Empty, nameof(SellerAddRequest.BankNumber)) { Label = "empty bank number is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<SellerAddRequest> clearField, string propertyName)
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

    public static IEnumerable<TheoryDataRow<Action<SellerAddRequest>, string, string>> GetPdfTextFieldTestData()
    {
        yield return new(r => r.CompanyNumber = "Dragon 🐉", nameof(SellerAddRequest.CompanyNumber), "Company Number") { Label = "emoji in company number is rejected and named" };
        yield return new(r => r.CompanyName = "Dragon 🐉", nameof(SellerAddRequest.CompanyName), "Company Name") { Label = "emoji in company name is rejected and named" };
        yield return new(r => r.Street = "Dragon 🐉", nameof(SellerAddRequest.Street), "Street") { Label = "emoji in street is rejected and named" };
        yield return new(r => r.City = "Dragon 🐉", nameof(SellerAddRequest.City), "City") { Label = "emoji in city is rejected and named" };
        yield return new(r => r.Email = "Dragon 🐉", nameof(SellerAddRequest.Email), "Email") { Label = "emoji in email is rejected and named" };
        yield return new(r => r.Phone = "Dragon 🐉", nameof(SellerAddRequest.Phone), "Phone") { Label = "emoji in phone is rejected and named" };
        yield return new(r => r.BankName = "Dragon 🐉", nameof(SellerAddRequest.BankName), "Bank Name") { Label = "emoji in bank name is rejected and named" };
        yield return new(r => r.BankNumber = "Dragon 🐉", nameof(SellerAddRequest.BankNumber), "Bank Number") { Label = "emoji in bank number is rejected and named" };
    }

    [Theory]
    [MemberData(nameof(GetPdfTextFieldTestData))]
    public void Validate_GivenCharacterThePdfCannotShow_IsInvalid(Action<SellerAddRequest> setField, string propertyName, string displayName)
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
