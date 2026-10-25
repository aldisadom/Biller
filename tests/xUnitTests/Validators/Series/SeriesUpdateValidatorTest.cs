using BillerContracts.Requests.Series;
using FluentAssertions;
using Validators.Series;

namespace xUnitTests.Validators.Series;

public class SeriesUpdateValidatorTest
{
    private readonly SeriesUpdateValidator _validator = new();

    private static SeriesUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        Name = "WF",
        NextNumber = 1
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

    public static IEnumerable<TheoryDataRow<Action<SeriesUpdateRequest>, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(SeriesUpdateRequest.Id)) { Label = "empty id is rejected" };
        yield return new(r => r.Name = string.Empty, nameof(SeriesUpdateRequest.Name)) { Label = "empty name is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<SeriesUpdateRequest> clearField, string propertyName)
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

    [Theory]
    [InlineData(1, true, Label = "next number 1 is accepted")]
    [InlineData(0, false, Label = "next number 0 is rejected")]
    [InlineData(-5, false, Label = "negative next number is rejected")]
    public void Validate_GivenNextNumber_ReturnsExpected(int nextNumber, bool expected)
    {
        //Arrange
        var request = ValidRequest();
        request.NextNumber = nextNumber;

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().Be(expected);
    }
}
