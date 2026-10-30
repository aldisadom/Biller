using BillerContracts.Requests.Series;
using FluentAssertions;
using Validators.Series;

namespace xUnitTests.Validators.Series;

public class SeriesAddValidatorTest
{
    private readonly SeriesAddValidator _validator = new();

    private static SeriesAddRequest ValidRequest() => new()
    {
        SellerId = Guid.NewGuid(),
        Name = "WF"
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

    public static IEnumerable<TheoryDataRow<Action<SeriesAddRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.SellerId = Guid.Empty, nameof(SeriesAddRequest.SellerId)) { Label = "empty seller id is rejected" };
        yield return new(r => r.Name = string.Empty, nameof(SeriesAddRequest.Name)) { Label = "empty name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<SeriesAddRequest> clearField, string propertyName)
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
    [InlineData("ABCDEFGHIJ", true, Label = "name of 10 characters is accepted")]
    [InlineData("ABCDEFGHIJK", false, Label = "name of 11 characters is rejected")]
    [InlineData(" ABCDEFGHIJ ", true, Label = "name of 10 characters with surrounding spaces is accepted, it is trimmed")]
    public void Validate_GivenNameLength_ReturnsExpected(string name, bool expected)
    {
        //Arrange
        var request = ValidRequest();
        request.Name = name;

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void Validate_GivenNullName_IsInvalidWithoutThrowing()
    {
        //Arrange
        var request = ValidRequest();
        request.Name = null!;

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SeriesAddRequest.Name));
    }
}
