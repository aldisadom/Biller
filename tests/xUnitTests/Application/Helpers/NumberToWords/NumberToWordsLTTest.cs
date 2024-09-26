using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class NumberToWordsLTTest
{
    private readonly NumberToWordsLT _numberToWordsLT;
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly Mock<ITensToWords> _tensToWordsMock;
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly Mock<IThousandsToWords> _thousandsToWordsMock;
    private readonly Mock<IMillionsToWords> _millionsToWordsMock;

    private readonly OnesToWordsLT _onesToWords;
    private readonly TensToWordsLT _tensToWords;
    private readonly HundredsToWordsLT _hundredsToWords;
    private readonly ThousandsToWordsLT _thousandsToWords;
    private readonly MillionsToWordsLT _millionsToWords;


    public NumberToWordsLTTest()
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

        _numberToWordsLT = new NumberToWordsLT(_onesToWordsMock.Object, _tensToWordsMock.Object, _hundredsToWordsMock.Object, _thousandsToWordsMock.Object, _millionsToWordsMock.Object);

        _onesToWords = new OnesToWordsLT();
        _tensToWords = new TensToWordsLT(_onesToWordsMock.Object);
        _hundredsToWords = new HundredsToWordsLT(_onesToWordsMock.Object, _tensToWordsMock.Object);
        _thousandsToWords = new ThousandsToWordsLT(_hundredsToWordsMock.Object);
        _millionsToWords = new MillionsToWordsLT(_hundredsToWordsMock.Object, _thousandsToWordsMock.Object);

    }

    [Fact]
    public void NumberToWordsOnesSplit_GivenValid_ReturnsResult()
    {
        //Arrange
        int number = 1;
        _onesToWordsMock.Setup(x => x.OnesSplit(number, false))
            .Returns("");

        //Act
        string result = _numberToWordsLT.OnesSplit(number, false);

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
        string result = _numberToWordsLT.TensSplit(number, false);

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
        string result = _numberToWordsLT.HundredsSplit(number, false);

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
        string result = _numberToWordsLT.ThousandsSplit(number, false);

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
        string result = _numberToWordsLT.MillionsSplit(number);

        //Assert
        _millionsToWordsMock.Verify(x => x.MillionsSplit(number), Times.Once());
    }

    [Theory]
    [InlineData(0, false, "nulis")]
    [InlineData(1, false, "vienas")]
    [InlineData(2, false, "du")]
    [InlineData(3, false, "trys")]
    [InlineData(4, false, "keturi")]
    [InlineData(5, false, "penki")]
    [InlineData(6, false, "šeši")]
    [InlineData(7, false, "septyni")]
    [InlineData(8, false, "aštuoni")]
    [InlineData(9, false, "devyni")]
    [InlineData(9, true, " devyni")]
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
    [InlineData(10, false, "dešimt")]
    [InlineData(11, false, "vienuolika")]
    [InlineData(12, false, "dvylika")]
    [InlineData(13, false, "trylika")]
    [InlineData(14, false, "keturiolika")]
    [InlineData(15, false, "penkiolika")]
    [InlineData(16, false, "šešiolika")]
    [InlineData(17, false, "septyniolika")]
    [InlineData(18, false, "aštuoniolika")]
    [InlineData(19, false, "devyniolika")]
    [InlineData(20, false, "dvidešimt [MockOne0]")]
    [InlineData(30, false, "trisdešimt [MockOne0]")]
    [InlineData(40, false, "[MockOne4]asdešimt [MockOne0]")]
    [InlineData(40, true, " [MockOne4]asdešimt [MockOne0]")]
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
    [InlineData(100, false, "[MockOne1] šimtas [MockTen0]")]
    [InlineData(200, false, "[MockOne2] šimtai [MockTen0]")]
    [InlineData(200, true, " [MockOne2] šimtai [MockTen0]")]
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
    [InlineData(1000, false, "[MockHundred1] tūkstantis [MockHundred0]")]
    [InlineData(2000, false, "[MockHundred2] tūkstančiai [MockHundred0]")]
    [InlineData(10000, false, "[MockHundred10] tūkstančių [MockHundred0]")]
    [InlineData(11000, false, "[MockHundred11] tūkstančių [MockHundred0]")]
    [InlineData(20000, false, "[MockHundred20] tūkstančių [MockHundred0]")]
    [InlineData(21000, false, "[MockHundred21] tūkstantis [MockHundred0]")]
    [InlineData(22000, false, "[MockHundred22] tūkstančiai [MockHundred0]")]
    [InlineData(30000, false, "[MockHundred30] tūkstančių [MockHundred0]")]
    [InlineData(31000, false, "[MockHundred31] tūkstantis [MockHundred0]")]
    [InlineData(32000, false, "[MockHundred32] tūkstančiai [MockHundred0]")]
    [InlineData(100000, false, "[MockHundred100] tūkstančių [MockHundred0]")]
    [InlineData(101000, false, "[MockHundred101] tūkstantis [MockHundred0]")]
    [InlineData(102000, false, "[MockHundred102] tūkstančiai [MockHundred0]")]
    [InlineData(110000, false, "[MockHundred110] tūkstančių [MockHundred0]")]
    [InlineData(111000, false, "[MockHundred111] tūkstančių [MockHundred0]")]
    [InlineData(120000, false, "[MockHundred120] tūkstančių [MockHundred0]")]
    [InlineData(121000, false, "[MockHundred121] tūkstantis [MockHundred0]")]
    [InlineData(122000, false, "[MockHundred122] tūkstančiai [MockHundred0]")]
    [InlineData(122000, true, " [MockHundred122] tūkstančiai [MockHundred0]")]
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
    [InlineData(1000000, "[MockHundred1] milijonas [MockThousand0]")]
    [InlineData(2000000, "[MockHundred2] milijonai [MockThousand0]")]
    [InlineData(10000000, "[MockHundred10] milijonų [MockThousand0]")]
    [InlineData(11000000, "[MockHundred11] milijonų [MockThousand0]")]
    [InlineData(20000000, "[MockHundred20] milijonų [MockThousand0]")]
    [InlineData(21000000, "[MockHundred21] milijonas [MockThousand0]")]
    [InlineData(22000000, "[MockHundred22] milijonai [MockThousand0]")]
    [InlineData(30000000, "[MockHundred30] milijonų [MockThousand0]")]
    [InlineData(31000000, "[MockHundred31] milijonas [MockThousand0]")]
    [InlineData(32000000, "[MockHundred32] milijonai [MockThousand0]")]
    [InlineData(100000000, "[MockHundred100] milijonų [MockThousand0]")]
    [InlineData(101000000, "[MockHundred101] milijonas [MockThousand0]")]
    [InlineData(102000000, "[MockHundred102] milijonai [MockThousand0]")]
    [InlineData(110000000, "[MockHundred110] milijonų [MockThousand0]")]
    [InlineData(111000000, "[MockHundred111] milijonų [MockThousand0]")]
    [InlineData(120000000, "[MockHundred120] milijonų [MockThousand0]")]
    [InlineData(121000000, "[MockHundred121] milijonas [MockThousand0]")]
    [InlineData(122000001, "[MockHundred122] milijonai [MockThousand1]")]
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
