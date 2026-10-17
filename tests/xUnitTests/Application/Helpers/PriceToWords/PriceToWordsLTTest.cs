using Application.Helpers.NumberToWords;
using Application.Helpers.PriceToWords;
using BillerContracts.Enums;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsLTTest
{
    private readonly global::Application.Helpers.PriceToWords.PriceToWords _priceToWords;
    private readonly Mock<INumberToWords> _numberToWordsMock;
    private readonly PriceToWordsFactory _priceToWordsFactory;

    public PriceToWordsLTTest()
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
    [InlineData(0, "nulis € 0 ct.", Label = "zero price reads as zero euro and zero cents")]
    [InlineData(0.10, "nulis € 10 ct.", Label = "10 cents with zero euro")]
    [InlineData(0.11, "nulis € 11 ct.", Label = "11 cents with zero euro")]
    [InlineData(0.111111, "nulis € 11 ct.", Label = "cents beyond two digits are truncated")]
    [InlineData(1, "vienas € 0 ct.", Label = "1 euro reads as vienas")]
    [InlineData(2, "du € 0 ct.", Label = "2 euro reads as du")]
    [InlineData(3, "trys € 0 ct.", Label = "3 euro reads as trys")]
    [InlineData(4, "keturi € 0 ct.", Label = "4 euro reads as keturi")]
    [InlineData(5, "penki € 0 ct.", Label = "5 euro reads as penki")]
    [InlineData(6, "šeši € 0 ct.", Label = "6 euro reads as šeši")]
    [InlineData(7, "septyni € 0 ct.", Label = "7 euro reads as septyni")]
    [InlineData(8, "aštuoni € 0 ct.", Label = "8 euro reads as aštuoni")]
    [InlineData(9, "devyni € 0 ct.", Label = "9 euro reads as devyni")]
    [InlineData(10, "dešimt € 0 ct.", Label = "10 euro reads as dešimt")]
    [InlineData(11, "vienuolika € 0 ct.", Label = "11 euro reads as vienuolika")]
    [InlineData(12, "dvylika € 0 ct.", Label = "12 euro reads as dvylika")]
    [InlineData(13, "trylika € 0 ct.", Label = "13 euro reads as trylika")]
    [InlineData(14, "keturiolika € 0 ct.", Label = "14 euro reads as keturiolika")]
    [InlineData(15, "penkiolika € 0 ct.", Label = "15 euro reads as penkiolika")]
    [InlineData(16, "šešiolika € 0 ct.", Label = "16 euro reads as šešiolika")]
    [InlineData(17, "septyniolika € 0 ct.", Label = "17 euro reads as septyniolika")]
    [InlineData(18, "aštuoniolika € 0 ct.", Label = "18 euro reads as aštuoniolika")]
    [InlineData(19, "devyniolika € 0 ct.", Label = "19 euro reads as devyniolika")]
    [InlineData(20, "dvidešimt € 0 ct.", Label = "20 euro reads as dvidešimt")]
    [InlineData(30, "trisdešimt € 0 ct.", Label = "30 euro reads as trisdešimt")]
    [InlineData(40, "keturiasdešimt € 0 ct.", Label = "40 euro reads as keturiasdešimt")]
    [InlineData(41, "keturiasdešimt vienas € 0 ct.", Label = "41 euro reads as keturiasdešimt vienas")]
    [InlineData(99, "devyniasdešimt devyni € 0 ct.", Label = "99 euro reads as devyniasdešimt devyni")]
    [InlineData(100, "vienas šimtas € 0 ct.", Label = "100 euro reads as vienas šimtas")]
    [InlineData(200, "du šimtai € 0 ct.", Label = "200 euro reads as du šimtai")]
    [InlineData(210, "du šimtai dešimt € 0 ct.", Label = "210 euro reads as du šimtai dešimt")]
    [InlineData(1000, "vienas tūkstantis € 0 ct.", Label = "1000 euro reads as vienas tūkstantis")]
    [InlineData(2000, "du tūkstančiai € 0 ct.", Label = "2000 euro reads as du tūkstančiai")]
    [InlineData(10000, "dešimt tūkstančių € 0 ct.", Label = "10000 euro reads as dešimt tūkstančių")]
    [InlineData(11000, "vienuolika tūkstančių € 0 ct.", Label = "11000 euro reads as vienuolika tūkstančių")]
    [InlineData(20000, "dvidešimt tūkstančių € 0 ct.", Label = "20000 euro reads as dvidešimt tūkstančių")]
    [InlineData(21000, "dvidešimt vienas tūkstantis € 0 ct.", Label = "21000 euro reads as dvidešimt vienas tūkstantis")]
    [InlineData(22000, "dvidešimt du tūkstančiai € 0 ct.", Label = "22000 euro reads as dvidešimt du tūkstančiai")]
    [InlineData(30000, "trisdešimt tūkstančių € 0 ct.", Label = "30000 euro reads as trisdešimt tūkstančių")]
    [InlineData(31000, "trisdešimt vienas tūkstantis € 0 ct.", Label = "31000 euro reads as trisdešimt vienas tūkstantis")]
    [InlineData(32000, "trisdešimt du tūkstančiai € 0 ct.", Label = "32000 euro reads as trisdešimt du tūkstančiai")]
    [InlineData(100000, "vienas šimtas tūkstančių € 0 ct.", Label = "100000 euro reads as vienas šimtas tūkstančių")]
    [InlineData(101000, "vienas šimtas vienas tūkstantis € 0 ct.", Label = "101000 euro reads as vienas šimtas vienas tūkstantis")]
    [InlineData(102000, "vienas šimtas du tūkstančiai € 0 ct.", Label = "102000 euro reads as vienas šimtas du tūkstančiai")]
    [InlineData(110000, "vienas šimtas dešimt tūkstančių € 0 ct.", Label = "110000 euro reads as vienas šimtas dešimt tūkstančių")]
    [InlineData(111000, "vienas šimtas vienuolika tūkstančių € 0 ct.", Label = "111000 euro reads as vienas šimtas vienuolika tūkstančių")]
    [InlineData(120000, "vienas šimtas dvidešimt tūkstančių € 0 ct.", Label = "120000 euro reads as vienas šimtas dvidešimt tūkstančių")]
    [InlineData(121000, "vienas šimtas dvidešimt vienas tūkstantis € 0 ct.", Label = "121000 euro reads as vienas šimtas dvidešimt vienas tūkstantis")]
    [InlineData(122000, "vienas šimtas dvidešimt du tūkstančiai € 0 ct.", Label = "122000 euro reads as vienas šimtas dvidešimt du tūkstančiai")]
    [InlineData(122001, "vienas šimtas dvidešimt du tūkstančiai vienas € 0 ct.", Label = "122001 euro reads as vienas šimtas dvidešimt du tūkstančiai vienas")]
    [InlineData(999999, "devyni šimtai devyniasdešimt devyni tūkstančiai devyni šimtai devyniasdešimt devyni € 0 ct.", Label = "999999 euro reads as devyni šimtai devyniasdešimt devyni tūkstančiai devyni šimtai devyniasdešimt devyni")]
    [InlineData(1000000, "vienas milijonas € 0 ct.", Label = "1000000 euro reads as vienas milijonas")]
    [InlineData(2000000, "du milijonai € 0 ct.", Label = "2000000 euro reads as du milijonai")]
    [InlineData(10000000, "dešimt milijonų € 0 ct.", Label = "10000000 euro reads as dešimt milijonų")]
    [InlineData(11000000, "vienuolika milijonų € 0 ct.", Label = "11000000 euro reads as vienuolika milijonų")]
    [InlineData(20000000, "dvidešimt milijonų € 0 ct.", Label = "20000000 euro reads as dvidešimt milijonų")]
    [InlineData(21000000, "dvidešimt vienas milijonas € 0 ct.", Label = "21000000 euro reads as dvidešimt vienas milijonas")]
    [InlineData(22000000, "dvidešimt du milijonai € 0 ct.", Label = "22000000 euro reads as dvidešimt du milijonai")]
    [InlineData(30000000, "trisdešimt milijonų € 0 ct.", Label = "30000000 euro reads as trisdešimt milijonų")]
    [InlineData(31000000, "trisdešimt vienas milijonas € 0 ct.", Label = "31000000 euro reads as trisdešimt vienas milijonas")]
    [InlineData(32000000, "trisdešimt du milijonai € 0 ct.", Label = "32000000 euro reads as trisdešimt du milijonai")]
    [InlineData(100000000, "vienas šimtas milijonų € 0 ct.", Label = "100000000 euro reads as vienas šimtas milijonų")]
    [InlineData(101000000, "vienas šimtas vienas milijonas € 0 ct.", Label = "101000000 euro reads as vienas šimtas vienas milijonas")]
    [InlineData(102000000, "vienas šimtas du milijonai € 0 ct.", Label = "102000000 euro reads as vienas šimtas du milijonai")]
    [InlineData(110000000, "vienas šimtas dešimt milijonų € 0 ct.", Label = "110000000 euro reads as vienas šimtas dešimt milijonų")]
    [InlineData(111000000, "vienas šimtas vienuolika milijonų € 0 ct.", Label = "111000000 euro reads as vienas šimtas vienuolika milijonų")]
    [InlineData(120000000, "vienas šimtas dvidešimt milijonų € 0 ct.", Label = "120000000 euro reads as vienas šimtas dvidešimt milijonų")]
    [InlineData(121000000, "vienas šimtas dvidešimt vienas milijonas € 0 ct.", Label = "121000000 euro reads as vienas šimtas dvidešimt vienas milijonas")]
    [InlineData(122000000, "vienas šimtas dvidešimt du milijonai € 0 ct.", Label = "122000000 euro reads as vienas šimtas dvidešimt du milijonai")]
    [InlineData(122000001, "vienas šimtas dvidešimt du milijonai vienas € 0 ct.", Label = "122000001 euro reads as vienas šimtas dvidešimt du milijonai vienas")]
    public void FromFactoryDecode_GivenValid_Returns(decimal number, string expected)
    {
        //Arrange
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(Language.LT);

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
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(Language.LT);

        //Act
        //Assert
        priceToWords.Invoking(x => x.Decode(number))
            .Should().Throw<ArgumentException>();
    }
}
