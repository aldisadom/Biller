using BillerContracts.Requests.User;
using FluentAssertions;
using Validators.User;

namespace xUnitTests.Validators.User;

public class UserUpdateValidatorTest
{
    private readonly UserUpdateValidator _validator = new();

    private static UserUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        Name = "John",
        LastName = "Doe"
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
        // Arrange
        var request = ValidRequest();
        clearField(request);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName);
    }
}
