using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class ThousandsToWordsENTest
{
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly ThousandsToWordsEN _thousandsToWords;

    public ThousandsToWordsENTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockHundred{number}]");

        _thousandsToWords = new ThousandsToWordsEN(_hundredsToWordsMock.Object);
    }

    [Theory]
    [InlineData(999, false, "[MockHundred999]", Label = "below one thousand is delegated to the hundreds converter")]
    [InlineData(1000, false, "[MockHundred1] thousand [MockHundred0]", Label = "1000 splits into 1 thousand and the hundreds remainder")]
    [InlineData(2000, false, "[MockHundred2] thousand [MockHundred0]", Label = "2000 splits into 2 thousand and the hundreds remainder")]
    [InlineData(10000, false, "[MockHundred10] thousand [MockHundred0]", Label = "10000 splits into 10 thousand and the hundreds remainder")]
    [InlineData(11000, false, "[MockHundred11] thousand [MockHundred0]", Label = "11000 splits into 11 thousand and the hundreds remainder")]
    [InlineData(20000, false, "[MockHundred20] thousand [MockHundred0]", Label = "20000 splits into 20 thousand and the hundreds remainder")]
    [InlineData(21000, false, "[MockHundred21] thousand [MockHundred0]", Label = "21000 splits into 21 thousand and the hundreds remainder")]
    [InlineData(22000, false, "[MockHundred22] thousand [MockHundred0]", Label = "22000 splits into 22 thousand and the hundreds remainder")]
    [InlineData(30000, false, "[MockHundred30] thousand [MockHundred0]", Label = "30000 splits into 30 thousand and the hundreds remainder")]
    [InlineData(31000, false, "[MockHundred31] thousand [MockHundred0]", Label = "31000 splits into 31 thousand and the hundreds remainder")]
    [InlineData(32000, false, "[MockHundred32] thousand [MockHundred0]", Label = "32000 splits into 32 thousand and the hundreds remainder")]
    [InlineData(100000, false, "[MockHundred100] thousand [MockHundred0]", Label = "100000 splits into 100 thousand and the hundreds remainder")]
    [InlineData(101000, false, "[MockHundred101] thousand [MockHundred0]", Label = "101000 splits into 101 thousand and the hundreds remainder")]
    [InlineData(102000, false, "[MockHundred102] thousand [MockHundred0]", Label = "102000 splits into 102 thousand and the hundreds remainder")]
    [InlineData(110000, false, "[MockHundred110] thousand [MockHundred0]", Label = "110000 splits into 110 thousand and the hundreds remainder")]
    [InlineData(111000, false, "[MockHundred111] thousand [MockHundred0]", Label = "111000 splits into 111 thousand and the hundreds remainder")]
    [InlineData(120000, false, "[MockHundred120] thousand [MockHundred0]", Label = "120000 splits into 120 thousand and the hundreds remainder")]
    [InlineData(121000, false, "[MockHundred121] thousand [MockHundred0]", Label = "121000 splits into 121 thousand and the hundreds remainder")]
    [InlineData(122000, false, "[MockHundred122] thousand [MockHundred0]", Label = "122000 splits into 122 thousand and the hundreds remainder")]
    [InlineData(122000, true, " [MockHundred122] thousand [MockHundred0]", Label = "122000 after a preceding word gets a leading space")]
    public void ThousandsSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _thousandsToWords.ThousandsSplit(number, hasBefore);

        //Assert
        result.Should().Be(expectedWord);
    }

    [Theory]
    [InlineData(1000000, Label = "1000000 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void ThousandsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        Action act = () => _thousandsToWords.ThousandsSplit(number, false);

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}
