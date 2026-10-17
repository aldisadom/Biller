using Application.Helpers.NumberToWords;
using Application.Helpers.PriceToWords;
using BillerContracts.Enums;
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
    [InlineData(1.00, "[MockNumber1] € 0 ct.", Label = "whole euro amount has zero cents")]
    [InlineData(1.99, "[MockNumber1] € 99 ct.", Label = "fraction is written as cents")]
    [InlineData(1.9911111, "[MockNumber1] € 99 ct.", Label = "fraction beyond two digits is truncated to cents")]
    [InlineData(1111111.00, "[MockNumber1111111] € 0 ct.", Label = "whole euro part is passed to the number converter unchanged")]
    public void Decode_Given_Returns(decimal number, string expected)
    {
        //Arrange
        //Act
        string result = _priceToWords.Decode(number);

        //Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(0, "zero € 0 ct.", Label = "zero price reads as zero euro and zero cents")]
    [InlineData(0.10, "zero € 10 ct.", Label = "10 cents with zero euro")]
    [InlineData(0.11, "zero € 11 ct.", Label = "11 cents with zero euro")]
    [InlineData(0.111111, "zero € 11 ct.", Label = "cents beyond two digits are truncated")]
    [InlineData(1, "one € 0 ct.", Label = "1 euro reads as one")]
    [InlineData(2, "two € 0 ct.", Label = "2 euro reads as two")]
    [InlineData(3, "three € 0 ct.", Label = "3 euro reads as three")]
    [InlineData(4, "four € 0 ct.", Label = "4 euro reads as four")]
    [InlineData(5, "five € 0 ct.", Label = "5 euro reads as five")]
    [InlineData(6, "six € 0 ct.", Label = "6 euro reads as six")]
    [InlineData(7, "seven € 0 ct.", Label = "7 euro reads as seven")]
    [InlineData(8, "eight € 0 ct.", Label = "8 euro reads as eight")]
    [InlineData(9, "nine € 0 ct.", Label = "9 euro reads as nine")]
    [InlineData(10, "ten € 0 ct.", Label = "10 euro reads as ten")]
    [InlineData(11, "eleven € 0 ct.", Label = "11 euro reads as eleven")]
    [InlineData(12, "twelve € 0 ct.", Label = "12 euro reads as twelve")]
    [InlineData(13, "thirteen € 0 ct.", Label = "13 euro reads as thirteen")]
    [InlineData(14, "fourteen € 0 ct.", Label = "14 euro reads as fourteen")]
    [InlineData(15, "fifteen € 0 ct.", Label = "15 euro reads as fifteen")]
    [InlineData(16, "sixteen € 0 ct.", Label = "16 euro reads as sixteen")]
    [InlineData(17, "seventeen € 0 ct.", Label = "17 euro reads as seventeen")]
    [InlineData(18, "eighteen € 0 ct.", Label = "18 euro reads as eighteen")]
    [InlineData(19, "nineteen € 0 ct.", Label = "19 euro reads as nineteen")]
    [InlineData(20, "twenty € 0 ct.", Label = "20 euro reads as twenty")]
    [InlineData(30, "thirty € 0 ct.", Label = "30 euro reads as thirty")]
    [InlineData(40, "forty € 0 ct.", Label = "40 euro reads as forty")]
    [InlineData(41, "forty one € 0 ct.", Label = "41 euro reads as forty one")]
    [InlineData(99, "ninety nine € 0 ct.", Label = "99 euro reads as ninety nine")]
    [InlineData(100, "one hundred € 0 ct.", Label = "100 euro reads as one hundred")]
    [InlineData(200, "two hundred € 0 ct.", Label = "200 euro reads as two hundred")]
    [InlineData(210, "two hundred ten € 0 ct.", Label = "210 euro reads as two hundred ten")]
    [InlineData(1000, "one thousand € 0 ct.", Label = "1000 euro reads as one thousand")]
    [InlineData(2000, "two thousand € 0 ct.", Label = "2000 euro reads as two thousand")]
    [InlineData(10000, "ten thousand € 0 ct.", Label = "10000 euro reads as ten thousand")]
    [InlineData(11000, "eleven thousand € 0 ct.", Label = "11000 euro reads as eleven thousand")]
    [InlineData(20000, "twenty thousand € 0 ct.", Label = "20000 euro reads as twenty thousand")]
    [InlineData(21000, "twenty one thousand € 0 ct.", Label = "21000 euro reads as twenty one thousand")]
    [InlineData(22000, "twenty two thousand € 0 ct.", Label = "22000 euro reads as twenty two thousand")]
    [InlineData(30000, "thirty thousand € 0 ct.", Label = "30000 euro reads as thirty thousand")]
    [InlineData(31000, "thirty one thousand € 0 ct.", Label = "31000 euro reads as thirty one thousand")]
    [InlineData(32000, "thirty two thousand € 0 ct.", Label = "32000 euro reads as thirty two thousand")]
    [InlineData(100000, "one hundred thousand € 0 ct.", Label = "100000 euro reads as one hundred thousand")]
    [InlineData(101000, "one hundred one thousand € 0 ct.", Label = "101000 euro reads as one hundred one thousand")]
    [InlineData(102000, "one hundred two thousand € 0 ct.", Label = "102000 euro reads as one hundred two thousand")]
    [InlineData(110000, "one hundred ten thousand € 0 ct.", Label = "110000 euro reads as one hundred ten thousand")]
    [InlineData(111000, "one hundred eleven thousand € 0 ct.", Label = "111000 euro reads as one hundred eleven thousand")]
    [InlineData(120000, "one hundred twenty thousand € 0 ct.", Label = "120000 euro reads as one hundred twenty thousand")]
    [InlineData(121000, "one hundred twenty one thousand € 0 ct.", Label = "121000 euro reads as one hundred twenty one thousand")]
    [InlineData(122000, "one hundred twenty two thousand € 0 ct.", Label = "122000 euro reads as one hundred twenty two thousand")]
    [InlineData(122001, "one hundred twenty two thousand one € 0 ct.", Label = "122001 euro reads as one hundred twenty two thousand one")]
    [InlineData(999999, "nine hundred ninety nine thousand nine hundred ninety nine € 0 ct.", Label = "999999 euro reads as nine hundred ninety nine thousand nine hundred ninety nine")]
    [InlineData(1000000, "one million € 0 ct.", Label = "1000000 euro reads as one million")]
    [InlineData(2000000, "two million € 0 ct.", Label = "2000000 euro reads as two million")]
    [InlineData(10000000, "ten million € 0 ct.", Label = "10000000 euro reads as ten million")]
    [InlineData(11000000, "eleven million € 0 ct.", Label = "11000000 euro reads as eleven million")]
    [InlineData(20000000, "twenty million € 0 ct.", Label = "20000000 euro reads as twenty million")]
    [InlineData(21000000, "twenty one million € 0 ct.", Label = "21000000 euro reads as twenty one million")]
    [InlineData(22000000, "twenty two million € 0 ct.", Label = "22000000 euro reads as twenty two million")]
    [InlineData(30000000, "thirty million € 0 ct.", Label = "30000000 euro reads as thirty million")]
    [InlineData(31000000, "thirty one million € 0 ct.", Label = "31000000 euro reads as thirty one million")]
    [InlineData(32000000, "thirty two million € 0 ct.", Label = "32000000 euro reads as thirty two million")]
    [InlineData(100000000, "one hundred million € 0 ct.", Label = "100000000 euro reads as one hundred million")]
    [InlineData(101000000, "one hundred one million € 0 ct.", Label = "101000000 euro reads as one hundred one million")]
    [InlineData(102000000, "one hundred two million € 0 ct.", Label = "102000000 euro reads as one hundred two million")]
    [InlineData(110000000, "one hundred ten million € 0 ct.", Label = "110000000 euro reads as one hundred ten million")]
    [InlineData(111000000, "one hundred eleven million € 0 ct.", Label = "111000000 euro reads as one hundred eleven million")]
    [InlineData(120000000, "one hundred twenty million € 0 ct.", Label = "120000000 euro reads as one hundred twenty million")]
    [InlineData(121000000, "one hundred twenty one million € 0 ct.", Label = "121000000 euro reads as one hundred twenty one million")]
    [InlineData(122000000, "one hundred twenty two million € 0 ct.", Label = "122000000 euro reads as one hundred twenty two million")]
    [InlineData(122000001, "one hundred twenty two million one € 0 ct.", Label = "122000001 euro reads as one hundred twenty two million one")]
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
    [InlineData(-1, Label = "negative price throws")]
    [InlineData(1000000001, Label = "price above one billion throws")]
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
