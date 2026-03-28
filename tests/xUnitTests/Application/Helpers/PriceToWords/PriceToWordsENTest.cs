using Application.Helpers.NumberToWords;
using Application.Helpers.PriceToWords;
using Contracts.Enums;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsENTest
{
    private readonly global::Application.Helpers.PriceToWords.PriceToWords _priceToWords;
    private readonly Mock<INumberToWords> _numberToWordsMock;
    private readonly PriceToWordsFactory _priceToWordsFactory;

    public PriceToWordsENTest()
    {
        _numberToWordsMock = new Mock<INumberToWords>(MockBehavior.Strict);
        _numberToWordsMock.Setup(x => x.MillionsSplit(It.IsAny<int>()))
            .Returns((int number) => { return $"[MockNumber{number}]"; });

        _priceToWords = new global::Application.Helpers.PriceToWords.PriceToWords(_numberToWordsMock.Object);
        _priceToWordsFactory = new();
    }

    [Theory]
    [InlineData(1.00, "[MockNumber1] € 0 ct.")]
    [InlineData(1.99, "[MockNumber1] € 99 ct.")]
    [InlineData(1.9911111, "[MockNumber1] € 99 ct.")]
    [InlineData(1111111.00, "[MockNumber1111111] € 0 ct.")]
    public void Decode_Given_Returns(decimal number, string expected)
    {
        //Arrange
        //Act
        string result = _priceToWords.Decode(number);

        //Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(0, "zero € 0 ct.")]
    [InlineData(0.10, "zero € 10 ct.")]
    [InlineData(0.11, "zero € 11 ct.")]
    [InlineData(0.111111, "zero € 11 ct.")]
    [InlineData(1, "one € 0 ct.")]
    [InlineData(2, "two € 0 ct.")]
    [InlineData(3, "three € 0 ct.")]
    [InlineData(4, "four € 0 ct.")]
    [InlineData(5, "five € 0 ct.")]
    [InlineData(6, "six € 0 ct.")]
    [InlineData(7, "seven € 0 ct.")]
    [InlineData(8, "eight € 0 ct.")]
    [InlineData(9, "nine € 0 ct.")]
    [InlineData(10, "ten € 0 ct.")]
    [InlineData(11, "eleven € 0 ct.")]
    [InlineData(12, "twelve € 0 ct.")]
    [InlineData(13, "thirteen € 0 ct.")]
    [InlineData(14, "fourteen € 0 ct.")]
    [InlineData(15, "fifteen € 0 ct.")]
    [InlineData(16, "sixteen € 0 ct.")]
    [InlineData(17, "seventeen € 0 ct.")]
    [InlineData(18, "eighteen € 0 ct.")]
    [InlineData(19, "nineteen € 0 ct.")]
    [InlineData(20, "twenty € 0 ct.")]
    [InlineData(30, "thirty € 0 ct.")]
    [InlineData(40, "forty € 0 ct.")]
    [InlineData(41, "forty one € 0 ct.")]
    [InlineData(99, "ninety nine € 0 ct.")]
    [InlineData(100, "one hundred € 0 ct.")]
    [InlineData(200, "two hundred € 0 ct.")]
    [InlineData(210, "two hundred ten € 0 ct.")]
    [InlineData(1000, "one thousand € 0 ct.")]
    [InlineData(2000, "two thousand € 0 ct.")]
    [InlineData(10000, "ten thousand € 0 ct.")]
    [InlineData(11000, "eleven thousand € 0 ct.")]
    [InlineData(20000, "twenty thousand € 0 ct.")]
    [InlineData(21000, "twenty one thousand € 0 ct.")]
    [InlineData(22000, "twenty two thousand € 0 ct.")]
    [InlineData(30000, "thirty thousand € 0 ct.")]
    [InlineData(31000, "thirty one thousand € 0 ct.")]
    [InlineData(32000, "thirty two thousand € 0 ct.")]
    [InlineData(100000, "one hundred thousand € 0 ct.")]
    [InlineData(101000, "one hundred one thousand € 0 ct.")]
    [InlineData(102000, "one hundred two thousand € 0 ct.")]
    [InlineData(110000, "one hundred ten thousand € 0 ct.")]
    [InlineData(111000, "one hundred eleven thousand € 0 ct.")]
    [InlineData(120000, "one hundred twenty thousand € 0 ct.")]
    [InlineData(121000, "one hundred twenty one thousand € 0 ct.")]
    [InlineData(122000, "one hundred twenty two thousand € 0 ct.")]
    [InlineData(122001, "one hundred twenty two thousand one € 0 ct.")]
    [InlineData(999999, "nine hundred ninety nine thousand nine hundred ninety nine € 0 ct.")]
    [InlineData(1000000, "one million € 0 ct.")]
    [InlineData(2000000, "two million € 0 ct.")]
    [InlineData(10000000, "ten million € 0 ct.")]
    [InlineData(11000000, "eleven million € 0 ct.")]
    [InlineData(20000000, "twenty million € 0 ct.")]
    [InlineData(21000000, "twenty one million € 0 ct.")]
    [InlineData(22000000, "twenty two million € 0 ct.")]
    [InlineData(30000000, "thirty million € 0 ct.")]
    [InlineData(31000000, "thirty one million € 0 ct.")]
    [InlineData(32000000, "thirty two million € 0 ct.")]
    [InlineData(100000000, "one hundred million € 0 ct.")]
    [InlineData(101000000, "one hundred one million € 0 ct.")]
    [InlineData(102000000, "one hundred two million € 0 ct.")]
    [InlineData(110000000, "one hundred ten million € 0 ct.")]
    [InlineData(111000000, "one hundred eleven million € 0 ct.")]
    [InlineData(120000000, "one hundred twenty million € 0 ct.")]
    [InlineData(121000000, "one hundred twenty one million € 0 ct.")]
    [InlineData(122000000, "one hundred twenty two million € 0 ct.")]
    [InlineData(122000001, "one hundred twenty two million one € 0 ct.")]
    public void FromFactoryDecode_GivenValid_Returns(decimal number, string expected)
    {
        //Arrange
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(Language.EN);

        //Act
        string result = priceToWords.Decode(number);

        //Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000000001)]
    public void FromFactoryDecode_GivenInValid_Throws(decimal number)
    {
        //Arrange
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(Language.EN);

        //Act
        //Assert
        priceToWords.Invoking(x => x.Decode(number))
            .Should().Throw<ArgumentException>();
    }
}
