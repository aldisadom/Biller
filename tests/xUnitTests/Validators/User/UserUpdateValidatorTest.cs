using BillerContracts.Requests.User;
using AwesomeAssertions;
using Validators.User;

namespace xUnitTests.Validators.User;

public class UserUpdateValidatorTest
{
    private readonly UserUpdateValidator _validator = new(RepoFonts.CalibriChecker);

    private static UserUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        Name = "John",
        LastName = "Doe"
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

    public static IEnumerable<TheoryDataRow<Action<UserUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(UserUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.Name = string.Empty, nameof(UserUpdateRequest.Name)) { Label = "empty name is rejected" };
        yield return new(r => r.LastName = string.Empty, nameof(UserUpdateRequest.LastName)) { Label = "empty last name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<UserUpdateRequest> clearField, string propertyName)
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

    public static IEnumerable<TheoryDataRow<Action<UserUpdateRequest>, string, string>> GetPdfTextFieldTestData()
    {
        yield return new(r => r.Name = "Dragon 🐉", nameof(UserUpdateRequest.Name), "Name") { Label = "emoji in name is rejected and named" };
        yield return new(r => r.LastName = "Dragon 🐉", nameof(UserUpdateRequest.LastName), "Last Name") { Label = "emoji in last name is rejected and named" };
    }

    [Theory]
    [MemberData(nameof(GetPdfTextFieldTestData))]
    public void Validate_GivenCharacterThePdfCannotShow_IsInvalid(Action<UserUpdateRequest> setField, string propertyName, string displayName)
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
        request.Name = "Ąžuolų šėtra €5";

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }
}
