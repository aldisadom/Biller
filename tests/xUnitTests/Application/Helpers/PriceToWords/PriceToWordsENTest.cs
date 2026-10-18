using Application.Helpers.NumberToWords;
using Application.Helpers.PriceToWords;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsENTest
{
    private readonly global::Application.Helpers.PriceToWords.PriceToWords _priceToWords;
    private readonly Mock<INumberToWords> _numberToWordsMock;

    public PriceToWordsENTest()
    {
        _numberToWordsMock = new Mock<INumberToWords>(MockBehavior.Strict);

        _priceToWords = new global::Application.Helpers.PriceToWords.PriceToWords(_numberToWordsMock.Object);
    }

    [Theory]
    [InlineData(1.00, 1, "[MockNumber1] € 0 ct.", Label = "whole euro amount has zero cents")]
    [InlineData(1.99, 1, "[MockNumber1] € 99 ct.", Label = "fraction is written as cents")]
    [InlineData(1.9911111, 1, "[MockNumber1] € 99 ct.", Label = "fraction beyond two digits is truncated to cents")]
    [InlineData(1111111.00, 1111111, "[MockNumber1111111] € 0 ct.", Label = "whole euro part is passed to the number converter unchanged")]
    public void Decode_GivenPrice_ReturnsWords(decimal number, int euros, string expected)
    {
        //Arrange
        _numberToWordsMock.Setup(x => x.MillionsSplit(euros))
            .Returns($"[MockNumber{euros}]");

        //Act
        string result = _priceToWords.Decode(number);

        //Assert
        result.Should().Be(expected);
        _numberToWordsMock.Verify(x => x.MillionsSplit(euros), Times.Once());
    }
}
