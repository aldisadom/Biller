using Application.Helpers.NumberToWords;
using FluentAssertions;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class OnesToWordsENTest
{
    private readonly OnesToWordsEN _onesToWords;

    public OnesToWordsENTest()
    {
        _onesToWords = new OnesToWordsEN();
    }

    [Theory]
    [InlineData(0, false, "zero", Label = "0 is spelled out as zero")]
    [InlineData(1, false, "one", Label = "1 is spelled out as one")]
    [InlineData(2, false, "two", Label = "2 is spelled out as two")]
    [InlineData(3, false, "three", Label = "3 is spelled out as three")]
    [InlineData(4, false, "four", Label = "4 is spelled out as four")]
    [InlineData(5, false, "five", Label = "5 is spelled out as five")]
    [InlineData(6, false, "six", Label = "6 is spelled out as six")]
    [InlineData(7, false, "seven", Label = "7 is spelled out as seven")]
    [InlineData(8, false, "eight", Label = "8 is spelled out as eight")]
    [InlineData(9, false, "nine", Label = "9 is spelled out as nine")]
    [InlineData(9, true, " nine", Label = "9 after a preceding word gets a leading space")]
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
