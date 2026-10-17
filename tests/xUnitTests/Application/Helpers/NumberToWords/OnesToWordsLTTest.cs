using Application.Helpers.NumberToWords;
using FluentAssertions;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class OnesToWordsLTTest
{
    private readonly OnesToWordsLT _onesToWords;

    public OnesToWordsLTTest()
    {
        _onesToWords = new OnesToWordsLT();
    }

    [Theory]
    [InlineData(0, false, "nulis", Label = "0 is spelled out as nulis")]
    [InlineData(1, false, "vienas", Label = "1 is spelled out as vienas")]
    [InlineData(2, false, "du", Label = "2 is spelled out as du")]
    [InlineData(3, false, "trys", Label = "3 is spelled out as trys")]
    [InlineData(4, false, "keturi", Label = "4 is spelled out as keturi")]
    [InlineData(5, false, "penki", Label = "5 is spelled out as penki")]
    [InlineData(6, false, "šeši", Label = "6 is spelled out as šeši")]
    [InlineData(7, false, "septyni", Label = "7 is spelled out as septyni")]
    [InlineData(8, false, "aštuoni", Label = "8 is spelled out as aštuoni")]
    [InlineData(9, false, "devyni", Label = "9 is spelled out as devyni")]
    [InlineData(9, true, " devyni", Label = "9 after a preceding word gets a leading space")]
    public void OnesSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _onesToWords.OnesSplit(number, hasBefore);

        //Assert
        result.Should().Be(expectedWord);
    }

    [Theory]
    [InlineData(11, Label = "11 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void OnesSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        Action act = () => _onesToWords.OnesSplit(number, false);

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}
