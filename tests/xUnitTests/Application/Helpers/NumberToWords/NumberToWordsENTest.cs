using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class NumberToWordsENTest
{
    private readonly NumberToWordsEN _numberToWordsEN;
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly Mock<ITensToWords> _tensToWordsMock;
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly Mock<IThousandsToWords> _thousandsToWordsMock;
    private readonly Mock<IMillionsToWords> _millionsToWordsMock;

    private readonly OnesToWordsEN _onesToWords;
    private readonly TensToWordsEN _tensToWords;
    private readonly HundredsToWordsEN _hundredsToWords;
    private readonly ThousandsToWordsEN _thousandsToWords;
    private readonly MillionsToWordsEN _millionsToWords;


    public NumberToWordsENTest()
    {
        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _tensToWordsMock = new Mock<ITensToWords>(MockBehavior.Strict);
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _thousandsToWordsMock = new Mock<IThousandsToWords>(MockBehavior.Strict);
        _millionsToWordsMock = new Mock<IMillionsToWords>(MockBehavior.Strict);

        _onesToWordsMock.Setup(x => x.OnesSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => { return $"{(hasBefore ? " " : "")}[MockOne{number}]"; });
        _tensToWordsMock.Setup(x => x.TensSplit(It.IsAny<int>(), It.IsAny<bool>()))
           .Returns((int number, bool hasBefore) => { return $"{(hasBefore ? " " : "")}[MockTen{number}]"; });
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(It.IsAny<int>(), It.IsAny<bool>()))
           .Returns((int number, bool hasBefore) => { return $"{(hasBefore ? " " : "")}[MockHundred{number}]"; });
        _thousandsToWordsMock.Setup(x => x.ThousandsSplit(It.IsAny<int>(), It.IsAny<bool>()))
           .Returns((int number, bool hasBefore) => { return $"{(hasBefore ? " " : "")}[MockThousand{number}]"; });

        _numberToWordsEN = new NumberToWordsEN(_onesToWordsMock.Object, _tensToWordsMock.Object, _hundredsToWordsMock.Object, _thousandsToWordsMock.Object, _millionsToWordsMock.Object);

        _onesToWords = new OnesToWordsEN();
        _tensToWords = new TensToWordsEN(_onesToWordsMock.Object);
        _hundredsToWords = new HundredsToWordsEN(_onesToWordsMock.Object, _tensToWordsMock.Object);
        _thousandsToWords = new ThousandsToWordsEN(_hundredsToWordsMock.Object);
        _millionsToWords = new MillionsToWordsEN(_hundredsToWordsMock.Object, _thousandsToWordsMock.Object);

    }

    [Fact]
    public void NumberToWordsOnesSplit_GivenValid_ReturnsResult()
    {
        //Arrange
        int number = 1;
        _onesToWordsMock.Setup(x => x.OnesSplit(number, false))
            .Returns("");

        //Act
        string result = _numberToWordsEN.OnesSplit(number, false);

        //Assert
        _onesToWordsMock.Verify(x => x.OnesSplit(number, false), Times.Once());
    }

    [Fact]
    public void NumberToWordsTensSplit_GivenValid_ReturnsResult()
    {
        //Arrange
        int number = 10;
        _tensToWordsMock.Setup(x => x.TensSplit(number, false))
            .Returns("");

        //Act
        string result = _numberToWordsEN.TensSplit(number, false);

        //Assert
        _tensToWordsMock.Verify(x => x.TensSplit(number, false), Times.Once());
    }

    [Fact]
    public void NumberToWordsHundredsSplit_GivenValid_ReturnsResult()
    {
        //Arrange
        int number = 100;
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(number, false))
            .Returns("");

        //Act
        string result = _numberToWordsEN.HundredsSplit(number, false);

        //Assert
        _hundredsToWordsMock.Verify(x => x.HundredsSplit(number, false), Times.Once());
    }

    [Fact]
    public void NumberToWordsThousandsSplit_GivenValid_ReturnsResult()
    {
        //Arrange
        int number = 1000;
        _thousandsToWordsMock.Setup(x => x.ThousandsSplit(number, false))
            .Returns("");

        //Act
        string result = _numberToWordsEN.ThousandsSplit(number, false);

        //Assert
        _thousandsToWordsMock.Verify(x => x.ThousandsSplit(number, false), Times.Once());
    }

    [Fact]
    public void NumberToWordsMillionsSplit_GivenValid_ReturnsResult()
    {
        //Arrange
        int number = 1000000;
        _millionsToWordsMock.Setup(x => x.MillionsSplit(number))
            .Returns("");

        //Act
        string result = _numberToWordsEN.MillionsSplit(number);

        //Assert
        _millionsToWordsMock.Verify(x => x.MillionsSplit(number), Times.Once());
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
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(11, Label = "11 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void OnesSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange        
        //Act
        //Assert
        _onesToWords.Invoking(x => x.OnesSplit(number, false))
            .Should().Throw<ArgumentException>();
    }


    [Theory]
    [InlineData(9, false, "[MockOne9]", Label = "single digit is delegated to the ones converter")]
    [InlineData(10, false, "ten", Label = "10 is spelled out as ten")]
    [InlineData(11, false, "eleven", Label = "11 is spelled out as eleven")]
    [InlineData(12, false, "twelve", Label = "12 is spelled out as twelve")]
    [InlineData(13, false, "thirteen", Label = "13 is spelled out as thirteen")]
    [InlineData(14, false, "fourteen", Label = "14 is spelled out as fourteen")]
    [InlineData(15, false, "fifteen", Label = "15 is spelled out as fifteen")]
    [InlineData(16, false, "sixteen", Label = "16 is spelled out as sixteen")]
    [InlineData(17, false, "seventeen", Label = "17 is spelled out as seventeen")]
    [InlineData(18, false, "eighteen", Label = "18 is spelled out as eighteen")]
    [InlineData(19, false, "nineteen", Label = "19 is spelled out as nineteen")]
    [InlineData(20, false, "twenty", Label = "20 is spelled out as twenty")]
    [InlineData(30, false, "thirty", Label = "30 is spelled out as thirty")]
    [InlineData(40, false, "forty", Label = "40 is spelled out as forty")]
    [InlineData(40, true, " forty", Label = "40 after a preceding word gets a leading space")]
    [InlineData(41, false, "forty [MockOne1]", Label = "41 joins the tens word with the ones converter")]
    [InlineData(50, false, "fifty", Label = "50 is spelled out as fifty")]
    [InlineData(60, false, "sixty", Label = "60 is spelled out as sixty")]
    [InlineData(70, false, "seventy", Label = "70 is spelled out as seventy")]
    [InlineData(80, false, "eighty", Label = "80 is spelled out as eighty")]
    [InlineData(90, false, "ninety", Label = "90 is spelled out as ninety")]
    public void TensSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _tensToWords.TensSplit(number, hasBefore);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(100, Label = "100 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void TensSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _tensToWords.Invoking(x => x.TensSplit(number, false))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(99, false, "[MockTen99]", Label = "below one hundred is delegated to the tens converter")]
    [InlineData(100, false, "[MockOne1] hundred [MockTen0]", Label = "100 uses the ones converter for the hundreds digit and the word hundred")]
    [InlineData(200, false, "[MockOne2] hundred [MockTen0]", Label = "200 uses the ones converter for the hundreds digit and the word hundred")]
    [InlineData(200, true, " [MockOne2] hundred [MockTen0]", Label = "200 after a preceding word gets a leading space")]
    public void HundredsSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _hundredsToWords.HundredsSplit(number, hasBefore);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(1000, Label = "1000 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void HundredsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _hundredsToWords.Invoking(x => x.HundredsSplit(number, false))
            .Should().Throw<ArgumentException>();
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
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(1000000, Label = "1000000 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void ThousandsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _thousandsToWords.Invoking(x => x.ThousandsSplit(number, false))
            .Should().Throw<ArgumentException>();
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
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(1000000000, Label = "1000000000 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void MillionsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _millionsToWords.Invoking(x => x.MillionsSplit(number))
            .Should().Throw<ArgumentException>();
    }
}
