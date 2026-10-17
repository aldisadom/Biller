using BillerContracts.Requests.User;
using FluentAssertions;
using Validators.User;

namespace xUnitTests.Validators.User;

public class UserLoginValidatorTest
{
    private readonly UserLoginValidator _validator = new();

    private static UserLoginRequest ValidRequest() => new()
    {
        Email = "user@example.com",
        Password = "somepassword"
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

    public static IEnumerable<TheoryDataRow<Action<UserLoginRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Email = string.Empty, nameof(UserLoginRequest.Email)) { Label = "empty email is rejected" };
        yield return new(r => r.Password = string.Empty, nameof(UserLoginRequest.Password)) { Label = "empty password is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<UserLoginRequest> clearField, string propertyName)
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
}
