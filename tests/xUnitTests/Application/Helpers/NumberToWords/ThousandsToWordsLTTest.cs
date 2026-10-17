using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class ThousandsToWordsLTTest
{
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly ThousandsToWordsLT _thousandsToWords;

    public ThousandsToWordsLTTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockHundred{number}]");

        _thousandsToWords = new ThousandsToWordsLT(_hundredsToWordsMock.Object);
    }

    [Theory]
    [InlineData(999, false, "[MockHundred999]", Label = "below one thousand is delegated to the hundreds converter")]
    [InlineData(1000, false, "[MockHundred1] tūkstantis [MockHundred0]", Label = "1000 uses the singular form tūkstantis because it ends in one")]
    [InlineData(2000, false, "[MockHundred2] tūkstančiai [MockHundred0]", Label = "2000 uses the plural form tūkstančiai because it ends in two to nine")]
    [InlineData(10000, false, "[MockHundred10] tūkstančių [MockHundred0]", Label = "10000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(11000, false, "[MockHundred11] tūkstančių [MockHundred0]", Label = "11000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(20000, false, "[MockHundred20] tūkstančių [MockHundred0]", Label = "20000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(21000, false, "[MockHundred21] tūkstantis [MockHundred0]", Label = "21000 uses the singular form tūkstantis because it ends in one")]
    [InlineData(22000, false, "[MockHundred22] tūkstančiai [MockHundred0]", Label = "22000 uses the plural form tūkstančiai because it ends in two to nine")]
    [InlineData(30000, false, "[MockHundred30] tūkstančių [MockHundred0]", Label = "30000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(31000, false, "[MockHundred31] tūkstantis [MockHundred0]", Label = "31000 uses the singular form tūkstantis because it ends in one")]
    [InlineData(32000, false, "[MockHundred32] tūkstančiai [MockHundred0]", Label = "32000 uses the plural form tūkstančiai because it ends in two to nine")]
    [InlineData(100000, false, "[MockHundred100] tūkstančių [MockHundred0]", Label = "100000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(101000, false, "[MockHundred101] tūkstantis [MockHundred0]", Label = "101000 uses the singular form tūkstantis because it ends in one")]
    [InlineData(102000, false, "[MockHundred102] tūkstančiai [MockHundred0]", Label = "102000 uses the plural form tūkstančiai because it ends in two to nine")]
    [InlineData(110000, false, "[MockHundred110] tūkstančių [MockHundred0]", Label = "110000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(111000, false, "[MockHundred111] tūkstančių [MockHundred0]", Label = "111000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(120000, false, "[MockHundred120] tūkstančių [MockHundred0]", Label = "120000 uses the genitive plural form tūkstančių because it ends in zero or a teen")]
    [InlineData(121000, false, "[MockHundred121] tūkstantis [MockHundred0]", Label = "121000 uses the singular form tūkstantis because it ends in one")]
    [InlineData(122000, false, "[MockHundred122] tūkstančiai [MockHundred0]", Label = "122000 uses the plural form tūkstančiai because it ends in two to nine")]
    [InlineData(122000, true, " [MockHundred122] tūkstančiai [MockHundred0]", Label = "122000 after a preceding word gets a leading space")]
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
