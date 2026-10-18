using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class NumberToWordsLTTest
{
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly Mock<ITensToWords> _tensToWordsMock;
    private readonly Mock<IHundredsToWords> _hundredsToWordsMock;
    private readonly Mock<IThousandsToWords> _thousandsToWordsMock;
    private readonly Mock<IMillionsToWords> _millionsToWordsMock;
    private readonly NumberToWordsLT _numberToWordsLT;

    public NumberToWordsLTTest()
    {
        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _tensToWordsMock = new Mock<ITensToWords>(MockBehavior.Strict);
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _thousandsToWordsMock = new Mock<IThousandsToWords>(MockBehavior.Strict);
        _millionsToWordsMock = new Mock<IMillionsToWords>(MockBehavior.Strict);

        _numberToWordsLT = new NumberToWordsLT(_onesToWordsMock.Object, _tensToWordsMock.Object, _hundredsToWordsMock.Object, _thousandsToWordsMock.Object, _millionsToWordsMock.Object);
    }

    [Fact]
    public void OnesSplit_GivenNumber_ReturnsResultOfOnesConverter()
    {
        //Arrange
        int number = 1;
        _onesToWordsMock.Setup(x => x.OnesSplit(number, false))
            .Returns("[ones]");

        //Act
        string result = _numberToWordsLT.OnesSplit(number, false);

        //Assert
        result.Should().Be("[ones]");

        _onesToWordsMock.Verify(x => x.OnesSplit(number, false), Times.Once());
    }

    [Fact]
    public void TensSplit_GivenNumber_ReturnsResultOfTensConverter()
    {
        //Arrange
        int number = 10;
        _tensToWordsMock.Setup(x => x.TensSplit(number, false))
            .Returns("[tens]");

        //Act
        string result = _numberToWordsLT.TensSplit(number, false);

        //Assert
        result.Should().Be("[tens]");

        _tensToWordsMock.Verify(x => x.TensSplit(number, false), Times.Once());
    }

    [Fact]
    public void HundredsSplit_GivenNumber_ReturnsResultOfHundredsConverter()
    {
        //Arrange
        int number = 100;
        _hundredsToWordsMock.Setup(x => x.HundredsSplit(number, false))
            .Returns("[hundreds]");

        //Act
        string result = _numberToWordsLT.HundredsSplit(number, false);

        //Assert
        result.Should().Be("[hundreds]");

        _hundredsToWordsMock.Verify(x => x.HundredsSplit(number, false), Times.Once());
    }

    [Fact]
    public void ThousandsSplit_GivenNumber_ReturnsResultOfThousandsConverter()
    {
        //Arrange
        int number = 1000;
        _thousandsToWordsMock.Setup(x => x.ThousandsSplit(number, false))
            .Returns("[thousands]");

        //Act
        string result = _numberToWordsLT.ThousandsSplit(number, false);

        //Assert
        result.Should().Be("[thousands]");

        _thousandsToWordsMock.Verify(x => x.ThousandsSplit(number, false), Times.Once());
    }

    [Fact]
    public void MillionsSplit_GivenNumber_ReturnsResultOfMillionsConverter()
    {
        //Arrange
        int number = 1000000;
        _millionsToWordsMock.Setup(x => x.MillionsSplit(number))
            .Returns("[millions]");

        //Act
        string result = _numberToWordsLT.MillionsSplit(number);

        //Assert
        result.Should().Be("[millions]");

        _millionsToWordsMock.Verify(x => x.MillionsSplit(number), Times.Once());
    }
}

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

public class TensToWordsLTTest
{
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly TensToWordsLT _tensToWords;

    public TensToWordsLTTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _onesToWordsMock.Setup(x => x.OnesSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockOne{number}]");

        _tensToWords = new TensToWordsLT(_onesToWordsMock.Object);
    }

    [Theory]
    [InlineData(9, false, "[MockOne9]", Label = "single digit is delegated to the ones converter")]
    [InlineData(10, false, "dešimt", Label = "10 is spelled out as dešimt")]
    [InlineData(11, false, "vienuolika", Label = "11 is spelled out as vienuolika")]
    [InlineData(12, false, "dvylika", Label = "12 is spelled out as dvylika")]
    [InlineData(13, false, "trylika", Label = "13 is spelled out as trylika")]
    [InlineData(14, false, "keturiolika", Label = "14 is spelled out as keturiolika")]
    [InlineData(15, false, "penkiolika", Label = "15 is spelled out as penkiolika")]
    [InlineData(16, false, "šešiolika", Label = "16 is spelled out as šešiolika")]
    [InlineData(17, false, "septyniolika", Label = "17 is spelled out as septyniolika")]
    [InlineData(18, false, "aštuoniolika", Label = "18 is spelled out as aštuoniolika")]
    [InlineData(19, false, "devyniolika", Label = "19 is spelled out as devyniolika")]
    [InlineData(20, false, "dvidešimt [MockOne0]", Label = "20 is spelled out as dvidešimt followed by the ones converter")]
    [InlineData(30, false, "trisdešimt [MockOne0]", Label = "30 is spelled out as trisdešimt followed by the ones converter")]
    [InlineData(40, false, "[MockOne4]asdešimt [MockOne0]", Label = "40 builds the tens word from the ones converter")]
    [InlineData(40, true, " [MockOne4]asdešimt [MockOne0]", Label = "40 after a preceding word gets a leading space")]
    public void TensSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _tensToWords.TensSplit(number, hasBefore);

        //Assert
        result.Should().Be(expectedWord);
    }

    [Theory]
    [InlineData(100, Label = "100 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void TensSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        Action act = () => _tensToWords.TensSplit(number, false);

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}

public class HundredsToWordsLTTest
{
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly Mock<ITensToWords> _tensToWordsMock;
    private readonly HundredsToWordsLT _hundredsToWords;

    public HundredsToWordsLTTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _onesToWordsMock.Setup(x => x.OnesSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockOne{number}]");
        _tensToWordsMock = new Mock<ITensToWords>(MockBehavior.Strict);
        _tensToWordsMock.Setup(x => x.TensSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockTen{number}]");

        _hundredsToWords = new HundredsToWordsLT(_onesToWordsMock.Object, _tensToWordsMock.Object);
    }

    [Theory]
    [InlineData(99, false, "[MockTen99]", Label = "below one hundred is delegated to the tens converter")]
    [InlineData(100, false, "[MockOne1] šimtas [MockTen0]", Label = "100 uses the ones converter for the hundreds digit and the word šimtas")]
    [InlineData(200, false, "[MockOne2] šimtai [MockTen0]", Label = "200 uses the ones converter for the hundreds digit and the word šimtai")]
    [InlineData(200, true, " [MockOne2] šimtai [MockTen0]", Label = "200 after a preceding word gets a leading space")]
    public void HundredsSplit_GivenValidNumber_ReturnsResult(int number, bool hasBefore, string expectedWord)
    {
        //Arrange
        //Act
        string result = _hundredsToWords.HundredsSplit(number, hasBefore);

        //Assert
        result.Should().Be(expectedWord);
    }

    [Theory]
    [InlineData(1000, Label = "1000 is above the supported range, throws")]
    [InlineData(-1, Label = "negative number throws")]
    public void HundredsSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange
        //Act
        Action act = () => _hundredsToWords.HundredsSplit(number, false);

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}

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
