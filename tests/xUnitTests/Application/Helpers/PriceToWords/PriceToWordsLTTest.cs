using Application.Helpers.NumberToWords;
using Application.Helpers.PriceToWords;
using Common.Enums;
using FluentAssertions;
using Moq;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsLTTest
{
    private readonly PriceToWordsLT _priceToWordsLT;
    private readonly Mock<INumberToWords> _numberToWordsMock;
    private readonly PriceToWordsFactory _priceToWordsFactory;

    public PriceToWordsLTTest()
    {
        _numberToWordsMock = new Mock<INumberToWords>(MockBehavior.Strict);
        _numberToWordsMock.Setup(x => x.MillionsSplit(It.IsAny<int>()))
            .Returns((int number) => { return $"[MockNumber{number}]"; });

        _priceToWordsLT = new PriceToWordsLT(_numberToWordsMock.Object);
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
        string result = _priceToWordsLT.Decode(number);

        //Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(0, "nulis € 0 ct.")]
    [InlineData(0.10, "nulis € 10 ct.")]
    [InlineData(0.11, "nulis € 11 ct.")]
    [InlineData(0.111111, "nulis € 11 ct.")]
    [InlineData(1, "vienas € 0 ct.")]
    [InlineData(2, "du € 0 ct.")]
    [InlineData(3, "trys € 0 ct.")]
    [InlineData(4, "keturi € 0 ct.")]
    [InlineData(5, "penki € 0 ct.")]
    [InlineData(6, "šeši € 0 ct.")]
    [InlineData(7, "septyni € 0 ct.")]
    [InlineData(8, "aštuoni € 0 ct.")]
    [InlineData(9, "devyni € 0 ct.")]
    [InlineData(10, "dešimt € 0 ct.")]
    [InlineData(11, "vienuolika € 0 ct.")]
    [InlineData(12, "dvylika € 0 ct.")]
    [InlineData(13, "trylika € 0 ct.")]
    [InlineData(14, "keturiolika € 0 ct.")]
    [InlineData(15, "penkiolika € 0 ct.")]
    [InlineData(16, "šešiolika € 0 ct.")]
    [InlineData(17, "septyniolika € 0 ct.")]
    [InlineData(18, "aštuoniolika € 0 ct.")]
    [InlineData(19, "devyniolika € 0 ct.")]
    [InlineData(20, "dvidešimt € 0 ct.")]
    [InlineData(30, "trisdešimt € 0 ct.")]
    [InlineData(40, "keturiasdešimt € 0 ct.")]
    [InlineData(41, "keturiasdešimt vienas € 0 ct.")]
    [InlineData(99, "devyniasdešimt devyni € 0 ct.")]
    [InlineData(100, "vienas šimtas € 0 ct.")]
    [InlineData(200, "du šimtai € 0 ct.")]
    [InlineData(210, "du šimtai dešimt € 0 ct.")]
    [InlineData(1000, "vienas tūkstantis € 0 ct.")]
    [InlineData(2000, "du tūkstančiai € 0 ct.")]
    [InlineData(10000, "dešimt tūkstančių € 0 ct.")]
    [InlineData(11000, "vienuolika tūkstančių € 0 ct.")]
    [InlineData(20000, "dvidešimt tūkstančių € 0 ct.")]
    [InlineData(21000, "dvidešimt vienas tūkstantis € 0 ct.")]
    [InlineData(22000, "dvidešimt du tūkstančiai € 0 ct.")]
    [InlineData(30000, "trisdešimt tūkstančių € 0 ct.")]
    [InlineData(31000, "trisdešimt vienas tūkstantis € 0 ct.")]
    [InlineData(32000, "trisdešimt du tūkstančiai € 0 ct.")]
    [InlineData(100000, "vienas šimtas tūkstančių € 0 ct.")]
    [InlineData(101000, "vienas šimtas vienas tūkstantis € 0 ct.")]
    [InlineData(102000, "vienas šimtas du tūkstančiai € 0 ct.")]
    [InlineData(110000, "vienas šimtas dešimt tūkstančių € 0 ct.")]
    [InlineData(111000, "vienas šimtas vienuolika tūkstančių € 0 ct.")]
    [InlineData(120000, "vienas šimtas dvidešimt tūkstančių € 0 ct.")]
    [InlineData(121000, "vienas šimtas dvidešimt vienas tūkstantis € 0 ct.")]
    [InlineData(122000, "vienas šimtas dvidešimt du tūkstančiai € 0 ct.")]
    [InlineData(122001, "vienas šimtas dvidešimt du tūkstančiai vienas € 0 ct.")]
    [InlineData(999999, "devyni šimtai devyniasdešimt devyni tūkstančiai devyni šimtai devyniasdešimt devyni € 0 ct.")]
    [InlineData(1000000, "vienas milijonas € 0 ct.")]
    [InlineData(2000000, "du milijonai € 0 ct.")]
    [InlineData(10000000, "dešimt milijonų € 0 ct.")]
    [InlineData(11000000, "vienuolika milijonų € 0 ct.")]
    [InlineData(20000000, "dvidešimt milijonų € 0 ct.")]
    [InlineData(21000000, "dvidešimt vienas milijonas € 0 ct.")]
    [InlineData(22000000, "dvidešimt du milijonai € 0 ct.")]
    [InlineData(30000000, "trisdešimt milijonų € 0 ct.")]
    [InlineData(31000000, "trisdešimt vienas milijonas € 0 ct.")]
    [InlineData(32000000, "trisdešimt du milijonai € 0 ct.")]
    [InlineData(100000000, "vienas šimtas milijonų € 0 ct.")]
    [InlineData(101000000, "vienas šimtas vienas milijonas € 0 ct.")]
    [InlineData(102000000, "vienas šimtas du milijonai € 0 ct.")]
    [InlineData(110000000, "vienas šimtas dešimt milijonų € 0 ct.")]
    [InlineData(111000000, "vienas šimtas vienuolika milijonų € 0 ct.")]
    [InlineData(120000000, "vienas šimtas dvidešimt milijonų € 0 ct.")]
    [InlineData(121000000, "vienas šimtas dvidešimt vienas milijonas € 0 ct.")]
    [InlineData(122000000, "vienas šimtas dvidešimt du milijonai € 0 ct.")]
    [InlineData(122000001, "vienas šimtas dvidešimt du milijonai vienas € 0 ct.")]
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
    [InlineData(-1)]
    [InlineData(1000000001)]
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
