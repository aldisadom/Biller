using BillerContracts.Requests.Customer;
using FluentAssertions;
using Validators.Customer;

namespace xUnitTests.Validators.Customer;

public class CustomerAddValidatorTest
{
    private readonly CustomerAddValidator _validator = new();

    private static CustomerAddRequest ValidRequest() => new()
    {
        SellerId = Guid.NewGuid(),
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

    public static IEnumerable<TheoryDataRow<Action<CustomerAddRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.SellerId = Guid.Empty, nameof(CustomerAddRequest.SellerId)) { Label = "empty seller id is rejected" };
        yield return new(r => r.CompanyNumber = string.Empty, nameof(CustomerAddRequest.CompanyNumber)) { Label = "empty company number is rejected" };
        yield return new(r => r.CompanyName = string.Empty, nameof(CustomerAddRequest.CompanyName)) { Label = "empty company name is rejected" };
        yield return new(r => r.Street = string.Empty, nameof(CustomerAddRequest.Street)) { Label = "empty street is rejected" };
        yield return new(r => r.City = string.Empty, nameof(CustomerAddRequest.City)) { Label = "empty city is rejected" };
        yield return new(r => r.State = string.Empty, nameof(CustomerAddRequest.State)) { Label = "empty state is rejected" };
        yield return new(r => r.Phone = string.Empty, nameof(CustomerAddRequest.Phone)) { Label = "empty phone is rejected" };
        yield return new(r => r.SeriesId = Guid.Empty, nameof(CustomerAddRequest.SeriesId)) { Label = "empty series id is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<CustomerAddRequest> clearField, string propertyName)
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
}
