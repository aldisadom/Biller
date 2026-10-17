using BillerContracts.Requests.Seller;
using FluentAssertions;
using Validators.Seller;

namespace xUnitTests.Validators.Seller;

public class SellerUpdateValidatorTest
{
    private readonly SellerUpdateValidator _validator = new();

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
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
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
