using Application.Helpers.NumberToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.NumberToWords;

public class HundredsToWordsENTest
{
    private readonly Mock<IOnesToWords> _onesToWordsMock;
    private readonly Mock<ITensToWords> _tensToWordsMock;
    private readonly HundredsToWordsEN _hundredsToWords;

    public HundredsToWordsENTest()
    {
        // The stubs write their arguments into the text they return (" [MockOne5]" is OnesSplit(5, true)),
        // so each expected string also checks which calls were made and with what arguments.
        _onesToWordsMock = new Mock<IOnesToWords>(MockBehavior.Strict);
        _onesToWordsMock.Setup(x => x.OnesSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockOne{number}]");
        _tensToWordsMock = new Mock<ITensToWords>(MockBehavior.Strict);
        _tensToWordsMock.Setup(x => x.TensSplit(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int number, bool hasBefore) => $"{(hasBefore ? " " : "")}[MockTen{number}]");

        _hundredsToWords = new HundredsToWordsEN(_onesToWordsMock.Object, _tensToWordsMock.Object);
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
