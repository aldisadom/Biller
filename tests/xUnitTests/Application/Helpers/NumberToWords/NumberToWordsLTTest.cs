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
