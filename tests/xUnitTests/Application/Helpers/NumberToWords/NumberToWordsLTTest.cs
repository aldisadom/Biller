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
        _numberToWordsLT = new NumberToWordsLT();

        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _tensToWordsMock = new Mock<ITensToWords>(MockBehavior.Strict);
        _hundredsToWordsMock = new Mock<IHundredsToWords>(MockBehavior.Strict);
        _thousandsToWordsMock = new Mock<IThousandsToWords>(MockBehavior.Strict);
        _millionsToWordsMock = new Mock<IMillionsToWords>(MockBehavior.Strict);
        
        _onesToWords = new OnesToWordsLT(_tensToWordsMock.Object);
        _tensToWords = new TensToWordsLT(_onesToWordsMock.Object, _hundredsToWordsMock.Object);
        _hundredsToWords = new HundredsToWordsLT(_onesToWordsMock.Object, _tensToWordsMock.Object, _thousandsToWordsMock.Object);
        _thousandsToWords = new ThousandsToWordsLT(_hundredsToWordsMock.Object,_millionsToWordsMock.Object);
        _millionsToWords = new MillionsToWordsLT(_hundredsToWordsMock.Object, _thousandsToWordsMock.Object);

    }

    [Theory]
    [InlineData(0, "nulis")]
    [InlineData(1, "vienas")]
    [InlineData(2, "du")]
    [InlineData(3, "trys")]
    [InlineData(4, "keturi")]
    [InlineData(5, "penki")]
    [InlineData(6, "šeši")]
    [InlineData(7, "septyni")]
    [InlineData(8, "aštuoni")]
    [InlineData(9, "devyni")]
    public void OnesSplit_GivenValidNumber_ReturnsResult(int number, string expectedWord)
    {
        //Arrange

        var sut = new OnesToWordsLT(_tensToWordsMock.Object);
        //Act
        string result = _numberToWordsLT.OnesSplit(number);

        //Assert
        result.Should().BeEquivalentTo(expectedWord);
        _tensToWordsMock.Verify(x => x.TensSplit(It.IsAny<int>()), Times.Never());
    }

    [Theory]
    [InlineData(11)]
    [InlineData(-1)]
    public void OnesSplit_GivenInvalidNumber_ThrowsException(int number)
    {
        //Arrange

        _tensToWordsMock.Setup(x => x.TensSplit(number))
            .Returns(expectedWord);
        //Act
        //Assert
        _numberToWordsLT.Invoking(x=>x.OnesSplit(number))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, "nulis")]
    [InlineData(1, "vienas")]
    [InlineData(2, "du")]
    [InlineData(3, "trys")]
    [InlineData(4, "keturi")]
    [InlineData(5, "penki")]
    [InlineData(6, "šeši")]
    [InlineData(7, "septyni")]
    [InlineData(8, "aštuoni")]
    [InlineData(9, "devyni")]
    public void TensSplit_GivenValidNumber_ReturnsResult(int number, string expectedWord)
    {
        //Arrange
        //Act
        string result = _numberToWordsLT.TensSplit(number);

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
        _numberToWordsLT.Invoking(x => x.OnesSplit(number))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, "nulis")]
    [InlineData(1, "vienas")]
    [InlineData(2, "du")]
    [InlineData(3, "trys")]
    [InlineData(4, "keturi")]
    [InlineData(5, "penki")]
    [InlineData(6, "šeši")]
    [InlineData(7, "septyni")]
    [InlineData(8, "aštuoni")]
    [InlineData(9, "devyni")]
    public void HundredsSplit_GivenValidNumber_ReturnsResult(int number, string expectedWord)
    {
        //Arrange
        //Act
        string result = _numberToWordsLT.HundredsSplit(number);

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
        _numberToWordsLT.Invoking(x => x.HundredsSplit(number))
            .Should().Throw<ArgumentException>();
    }
}
