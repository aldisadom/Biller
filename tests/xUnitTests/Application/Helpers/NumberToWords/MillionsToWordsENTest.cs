using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class MillionsToWordsENTest
{
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly Mock<IThousandsToWords> _thousandsToWordsMock;
    private readonly MillionsToWordsEN _millionsToWords;

    public MillionsToWordsENTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockHundred{number}]");
        _thousandsToWordsMock = new Mock<IThousandsToWords>(MockBehavior.Strict);
        _thousandsToWordsMock.Setup(x => x.ThousandsSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockThousand{number}]");

        _millionsToWords = new MillionsToWordsEN(_hundredsToWordsMock.Object, _thousandsToWordsMock.Object);
    }

    [Theory]
    [InlineData(999999, "[MockThousand999999]", Label = "below one million is delegated to the thousands converter")]
    [InlineData(1000000, "[MockHundred1] million [MockThousand0]", Label = "1000000 splits into 1 million and the thousands remainder")]
    [InlineData(2000000, "[MockHundred2] million [MockThousand0]", Label = "2000000 splits into 2 million and the thousands remainder")]
    [InlineData(10000000, "[MockHundred10] million [MockThousand0]", Label = "10000000 splits into 10 million and the thousands remainder")]
    [InlineData(11000000, "[MockHundred11] million [MockThousand0]", Label = "11000000 splits into 11 million and the thousands remainder")]
    [InlineData(20000000, "[MockHundred20] million [MockThousand0]", Label = "20000000 splits into 20 million and the thousands remainder")]
    [InlineData(21000000, "[MockHundred21] million [MockThousand0]", Label = "21000000 splits into 21 million and the thousands remainder")]
    [InlineData(22000000, "[MockHundred22] million [MockThousand0]", Label = "22000000 splits into 22 million and the thousands remainder")]
    [InlineData(30000000, "[MockHundred30] million [MockThousand0]", Label = "30000000 splits into 30 million and the thousands remainder")]
    [InlineData(31000000, "[MockHundred31] million [MockThousand0]", Label = "31000000 splits into 31 million and the thousands remainder")]
    [InlineData(32000000, "[MockHundred32] million [MockThousand0]", Label = "32000000 splits into 32 million and the thousands remainder")]
    [InlineData(100000000, "[MockHundred100] million [MockThousand0]", Label = "100000000 splits into 100 million and the thousands remainder")]
    [InlineData(101000000, "[MockHundred101] million [MockThousand0]", Label = "101000000 splits into 101 million and the thousands remainder")]
    [InlineData(102000000, "[MockHundred102] million [MockThousand0]", Label = "102000000 splits into 102 million and the thousands remainder")]
    [InlineData(110000000, "[MockHundred110] million [MockThousand0]", Label = "110000000 splits into 110 million and the thousands remainder")]
    [InlineData(111000000, "[MockHundred111] million [MockThousand0]", Label = "111000000 splits into 111 million and the thousands remainder")]
    [InlineData(120000000, "[MockHundred120] million [MockThousand0]", Label = "120000000 splits into 120 million and the thousands remainder")]
    [InlineData(121000000, "[MockHundred121] million [MockThousand0]", Label = "121000000 splits into 121 million and the thousands remainder")]
    [InlineData(122000001, "[MockHundred122] million [MockThousand1]", Label = "122000001 splits into 122 million and the thousands remainder")]
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
