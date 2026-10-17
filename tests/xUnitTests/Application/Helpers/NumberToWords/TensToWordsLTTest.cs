using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

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
