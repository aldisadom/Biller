using BillerContracts.Requests.Seller;
using FluentAssertions;
using Validators.Seller;

namespace xUnitTests.Validators.Seller;

public class SellerAddValidatorTest
{
    private readonly SellerAddValidator _validator = new();

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
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
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
    [InlineData("notanemail", Label = "text without an at sign is rejected")]
    [InlineData("@nodomain.com", Label = "address without a local part is rejected")]
    [InlineData("trailing.dot@example.com.", Label = "domain ending with a dot is rejected")]
    [InlineData("", Label = "empty email is rejected")]
    public void Validate_GivenInvalidEmail_IsInvalid(string email)
    {
        // Arrange
        var request = ValidRequest();
        request.Email = email;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }
}
