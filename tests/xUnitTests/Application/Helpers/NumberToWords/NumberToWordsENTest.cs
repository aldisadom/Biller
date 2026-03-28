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
    [InlineData(0, false, "zero")]
    [InlineData(1, false, "one")]
    [InlineData(2, false, "two")]
    [InlineData(3, false, "three")]
    [InlineData(4, false, "four")]
    [InlineData(5, false, "five")]
    [InlineData(6, false, "six")]
    [InlineData(7, false, "seven")]
    [InlineData(8, false, "eight")]
    [InlineData(9, false, "nine")]
    [InlineData(9, true, " nine")]
    public void OnesSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _onesToWords.OnesSplit(number, hasBefore);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(-1)]
    public void OnesSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange        
        //Act
        //Assert
        _onesToWords.Invoking(x => x.OnesSplit(number, false))
            .Should().Throw<ArgumentException>();
    }


    [Theory]
    [InlineData(9, false, "[MockOne9]")]
    [InlineData(10, false, "ten")]
    [InlineData(11, false, "eleven")]
    [InlineData(12, false, "twelve")]
    [InlineData(13, false, "thirteen")]
    [InlineData(14, false, "fourteen")]
    [InlineData(15, false, "fifteen")]
    [InlineData(16, false, "sixteen")]
    [InlineData(17, false, "seventeen")]
    [InlineData(18, false, "eighteen")]
    [InlineData(19, false, "nineteen")]
    [InlineData(20, false, "twenty")]
    [InlineData(30, false, "thirty")]
    [InlineData(40, false, "forty")]
    [InlineData(40, true, " forty")]
    [InlineData(41, false, "forty [MockOne1]")]
    [InlineData(50, false, "fifty")]
    [InlineData(60, false, "sixty")]
    [InlineData(70, false, "seventy")]
    [InlineData(80, false, "eighty")]
    [InlineData(90, false, "ninety")]
public void TensSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _tensToWords.TensSplit(number, hasBefore);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(-1)]
    public void TensSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _tensToWords.Invoking(x => x.TensSplit(number, false))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(99, false, "[MockTen99]")]
    [InlineData(100, false, "[MockOne1] hundred [MockTen0]")]
    [InlineData(200, false, "[MockOne2] hundred [MockTen0]")]
    [InlineData(200, true, " [MockOne2] hundred [MockTen0]")]
    public void HundredsSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _hundredsToWords.HundredsSplit(number, hasBefore);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(-1)]
    public void HundredsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _hundredsToWords.Invoking(x => x.HundredsSplit(number, false))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(999, false, "[MockHundred999]")]
    [InlineData(1000, false, "[MockHundred1] thousand [MockHundred0]")]
    [InlineData(2000, false, "[MockHundred2] thousand [MockHundred0]")]
    [InlineData(10000, false, "[MockHundred10] thousand [MockHundred0]")]
    [InlineData(11000, false, "[MockHundred11] thousand [MockHundred0]")]
    [InlineData(20000, false, "[MockHundred20] thousand [MockHundred0]")]
    [InlineData(21000, false, "[MockHundred21] thousand [MockHundred0]")]
    [InlineData(22000, false, "[MockHundred22] thousand [MockHundred0]")]
    [InlineData(30000, false, "[MockHundred30] thousand [MockHundred0]")]
    [InlineData(31000, false, "[MockHundred31] thousand [MockHundred0]")]
    [InlineData(32000, false, "[MockHundred32] thousand [MockHundred0]")]
    [InlineData(100000, false, "[MockHundred100] thousand [MockHundred0]")]
    [InlineData(101000, false, "[MockHundred101] thousand [MockHundred0]")]
    [InlineData(102000, false, "[MockHundred102] thousand [MockHundred0]")]
    [InlineData(110000, false, "[MockHundred110] thousand [MockHundred0]")]
    [InlineData(111000, false, "[MockHundred111] thousand [MockHundred0]")]
    [InlineData(120000, false, "[MockHundred120] thousand [MockHundred0]")]
    [InlineData(121000, false, "[MockHundred121] thousand [MockHundred0]")]
    [InlineData(122000, false, "[MockHundred122] thousand [MockHundred0]")]
    [InlineData(122000, true, " [MockHundred122] thousand [MockHundred0]")]
    public void ThousandsSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _thousandsToWords.ThousandsSplit(number, hasBefore);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(1000000)]
    [InlineData(-1)]
    public void ThousandsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _thousandsToWords.Invoking(x => x.ThousandsSplit(number, false))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(999999, "[MockThousand999999]")]
    [InlineData(1000000, "[MockHundred1] million [MockThousand0]")]
    [InlineData(2000000, "[MockHundred2] million [MockThousand0]")]
    [InlineData(10000000, "[MockHundred10] million [MockThousand0]")]
    [InlineData(11000000, "[MockHundred11] million [MockThousand0]")]
    [InlineData(20000000, "[MockHundred20] million [MockThousand0]")]
    [InlineData(21000000, "[MockHundred21] million [MockThousand0]")]
    [InlineData(22000000, "[MockHundred22] million [MockThousand0]")]
    [InlineData(30000000, "[MockHundred30] million [MockThousand0]")]
    [InlineData(31000000, "[MockHundred31] million [MockThousand0]")]
    [InlineData(32000000, "[MockHundred32] million [MockThousand0]")]
    [InlineData(100000000, "[MockHundred100] million [MockThousand0]")]
    [InlineData(101000000, "[MockHundred101] million [MockThousand0]")]
    [InlineData(102000000, "[MockHundred102] million [MockThousand0]")]
    [InlineData(110000000, "[MockHundred110] million [MockThousand0]")]
    [InlineData(111000000, "[MockHundred111] million [MockThousand0]")]
    [InlineData(120000000, "[MockHundred120] million [MockThousand0]")]
    [InlineData(121000000, "[MockHundred121] million [MockThousand0]")]
    [InlineData(122000001, "[MockHundred122] million [MockThousand1]")]
    public void MillionsSplit_GivenValidNumber_ReturnsResult(int number, string expectedWord)
    {
        //Arrange
        //Act
        string result = _millionsToWords.MillionsSplit(number);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
    }

    [Theory]
    [InlineData(1000000000)]
    [InlineData(-1)]
    public void MillionsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        //Assert
        _millionsToWords.Invoking(x => x.MillionsSplit(number))
            .Should().Throw<ArgumentException>();
    }
}
