using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class TensToWordsENTest
{
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly TensToWordsEN _tensToWords;

    public TensToWordsENTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _onesToWordsMock.Setup(x => x.OnesSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockOne{number}]");

        _tensToWords = new TensToWordsEN(_onesToWordsMock.Object);
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
