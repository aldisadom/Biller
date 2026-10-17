using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class MillionsToWordsLTTest
{
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly Mock<IThousandsToWords> _thousandsToWordsMock;
    private readonly MillionsToWordsLT _millionsToWords;

    public MillionsToWordsLTTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockHundred{number}]");
        _thousandsToWordsMock = new Mock<IThousandsToWords>(MockBehavior.Strict);
        _thousandsToWordsMock.Setup(x => x.ThousandsSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockThousand{number}]");

        _millionsToWords = new MillionsToWordsLT(_hundredsToWordsMock.Object, _thousandsToWordsMock.Object);
    }

    [Theory]
    [InlineData(999999, "[MockThousand999999]", Label = "below one million is delegated to the thousands converter")]
    [InlineData(1000000, "[MockHundred1] milijonas [MockThousand0]", Label = "1000000 uses the singular form milijonas because it ends in one")]
    [InlineData(2000000, "[MockHundred2] milijonai [MockThousand0]", Label = "2000000 uses the plural form milijonai because it ends in two to nine")]
    [InlineData(10000000, "[MockHundred10] milijonų [MockThousand0]", Label = "10000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(11000000, "[MockHundred11] milijonų [MockThousand0]", Label = "11000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(20000000, "[MockHundred20] milijonų [MockThousand0]", Label = "20000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(21000000, "[MockHundred21] milijonas [MockThousand0]", Label = "21000000 uses the singular form milijonas because it ends in one")]
    [InlineData(22000000, "[MockHundred22] milijonai [MockThousand0]", Label = "22000000 uses the plural form milijonai because it ends in two to nine")]
    [InlineData(30000000, "[MockHundred30] milijonų [MockThousand0]", Label = "30000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(31000000, "[MockHundred31] milijonas [MockThousand0]", Label = "31000000 uses the singular form milijonas because it ends in one")]
    [InlineData(32000000, "[MockHundred32] milijonai [MockThousand0]", Label = "32000000 uses the plural form milijonai because it ends in two to nine")]
    [InlineData(100000000, "[MockHundred100] milijonų [MockThousand0]", Label = "100000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(101000000, "[MockHundred101] milijonas [MockThousand0]", Label = "101000000 uses the singular form milijonas because it ends in one")]
    [InlineData(102000000, "[MockHundred102] milijonai [MockThousand0]", Label = "102000000 uses the plural form milijonai because it ends in two to nine")]
    [InlineData(110000000, "[MockHundred110] milijonų [MockThousand0]", Label = "110000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(111000000, "[MockHundred111] milijonų [MockThousand0]", Label = "111000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(120000000, "[MockHundred120] milijonų [MockThousand0]", Label = "120000000 uses the genitive plural form milijonų because it ends in zero or a teen")]
    [InlineData(121000000, "[MockHundred121] milijonas [MockThousand0]", Label = "121000000 uses the singular form milijonas because it ends in one")]
    [InlineData(122000001, "[MockHundred122] milijonai [MockThousand1]", Label = "122000001 uses the plural form milijonai because it ends in two to nine")]
    public void MillionsSplit_GivenValidNumber_ReturnsResult(int number, string expectedWord)
    {
        //Arrange
        //Act
        string result = _millionsToWords.MillionsSplit(number);

        //Assert
        result.Should().Be(expectedWord);
    }

    [Theory]
    [InlineData(1000000000, Label = "1000000000 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void MillionsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        Action act = () => _millionsToWords.MillionsSplit(number);

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}
