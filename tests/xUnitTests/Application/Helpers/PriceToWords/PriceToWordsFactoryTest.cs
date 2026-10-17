using Application.Helpers.PriceToWords;
using BillerContracts.Enums;
using FluentAssertions;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsFactoryTest
{
    private readonly PriceToWordsFactory _priceToWordsFactory;

    public PriceToWordsFactoryTest()
    {
        _priceToWordsFactory = new();
    }

    [Theory]
    [InlineData(Language.EN, Label = "english returns the price to words converter")]
    [InlineData(Language.LT, Label = "lithuanian returns the price to words converter")]
    public void GetConverter_GivenSupportedLanguage_ReturnsPriceToWords(Language language)
    {
        //Arrange
        //Act
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(language);

        //Assert
        priceToWords.Should().BeOfType<global::Application.Helpers.PriceToWords.PriceToWords>();
    }

    [Fact]
    public void GetConverter_GivenUnsupportedLanguage_ThrowsNotSupportedException()
    {
        //Arrange
        //Act
        Action act = () => _priceToWordsFactory.GetConverter((Language)999);

        //Assert
        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(Language.EN, 0, "zero € 0 ct.", Label = "in english, zero price reads as zero euro and zero cents")]
    [InlineData(Language.EN, 0.10, "zero € 10 ct.", Label = "in english, 10 cents with zero euro")]
    [InlineData(Language.EN, 0.11, "zero € 11 ct.", Label = "in english, 11 cents with zero euro")]
    [InlineData(Language.EN, 0.111111, "zero € 11 ct.", Label = "in english, cents beyond two digits are truncated")]
    [InlineData(Language.EN, 1, "one € 0 ct.", Label = "in english, 1 euro reads as one")]
    [InlineData(Language.EN, 2, "two € 0 ct.", Label = "in english, 2 euro reads as two")]
    [InlineData(Language.EN, 3, "three € 0 ct.", Label = "in english, 3 euro reads as three")]
    [InlineData(Language.EN, 4, "four € 0 ct.", Label = "in english, 4 euro reads as four")]
    [InlineData(Language.EN, 5, "five € 0 ct.", Label = "in english, 5 euro reads as five")]
    [InlineData(Language.EN, 6, "six € 0 ct.", Label = "in english, 6 euro reads as six")]
    [InlineData(Language.EN, 7, "seven € 0 ct.", Label = "in english, 7 euro reads as seven")]
    [InlineData(Language.EN, 8, "eight € 0 ct.", Label = "in english, 8 euro reads as eight")]
    [InlineData(Language.EN, 9, "nine € 0 ct.", Label = "in english, 9 euro reads as nine")]
    [InlineData(Language.EN, 10, "ten € 0 ct.", Label = "in english, 10 euro reads as ten")]
    [InlineData(Language.EN, 11, "eleven € 0 ct.", Label = "in english, 11 euro reads as eleven")]
    [InlineData(Language.EN, 12, "twelve € 0 ct.", Label = "in english, 12 euro reads as twelve")]
    [InlineData(Language.EN, 13, "thirteen € 0 ct.", Label = "in english, 13 euro reads as thirteen")]
    [InlineData(Language.EN, 14, "fourteen € 0 ct.", Label = "in english, 14 euro reads as fourteen")]
    [InlineData(Language.EN, 15, "fifteen € 0 ct.", Label = "in english, 15 euro reads as fifteen")]
    [InlineData(Language.EN, 16, "sixteen € 0 ct.", Label = "in english, 16 euro reads as sixteen")]
    [InlineData(Language.EN, 17, "seventeen € 0 ct.", Label = "in english, 17 euro reads as seventeen")]
    [InlineData(Language.EN, 18, "eighteen € 0 ct.", Label = "in english, 18 euro reads as eighteen")]
    [InlineData(Language.EN, 19, "nineteen € 0 ct.", Label = "in english, 19 euro reads as nineteen")]
    [InlineData(Language.EN, 20, "twenty € 0 ct.", Label = "in english, 20 euro reads as twenty")]
    [InlineData(Language.EN, 30, "thirty € 0 ct.", Label = "in english, 30 euro reads as thirty")]
    [InlineData(Language.EN, 40, "forty € 0 ct.", Label = "in english, 40 euro reads as forty")]
    [InlineData(Language.EN, 41, "forty one € 0 ct.", Label = "in english, 41 euro reads as forty one")]
    [InlineData(Language.EN, 99, "ninety nine € 0 ct.", Label = "in english, 99 euro reads as ninety nine")]
    [InlineData(Language.EN, 100, "one hundred € 0 ct.", Label = "in english, 100 euro reads as one hundred")]
    [InlineData(Language.EN, 200, "two hundred € 0 ct.", Label = "in english, 200 euro reads as two hundred")]
    [InlineData(Language.EN, 210, "two hundred ten € 0 ct.", Label = "in english, 210 euro reads as two hundred ten")]
    [InlineData(Language.EN, 1000, "one thousand € 0 ct.", Label = "in english, 1000 euro reads as one thousand")]
    [InlineData(Language.EN, 2000, "two thousand € 0 ct.", Label = "in english, 2000 euro reads as two thousand")]
    [InlineData(Language.EN, 10000, "ten thousand € 0 ct.", Label = "in english, 10000 euro reads as ten thousand")]
    [InlineData(Language.EN, 11000, "eleven thousand € 0 ct.", Label = "in english, 11000 euro reads as eleven thousand")]
    [InlineData(Language.EN, 20000, "twenty thousand € 0 ct.", Label = "in english, 20000 euro reads as twenty thousand")]
    [InlineData(Language.EN, 21000, "twenty one thousand € 0 ct.", Label = "in english, 21000 euro reads as twenty one thousand")]
    [InlineData(Language.EN, 22000, "twenty two thousand € 0 ct.", Label = "in english, 22000 euro reads as twenty two thousand")]
    [InlineData(Language.EN, 30000, "thirty thousand € 0 ct.", Label = "in english, 30000 euro reads as thirty thousand")]
    [InlineData(Language.EN, 31000, "thirty one thousand € 0 ct.", Label = "in english, 31000 euro reads as thirty one thousand")]
    [InlineData(Language.EN, 32000, "thirty two thousand € 0 ct.", Label = "in english, 32000 euro reads as thirty two thousand")]
    [InlineData(Language.EN, 100000, "one hundred thousand € 0 ct.", Label = "in english, 100000 euro reads as one hundred thousand")]
    [InlineData(Language.EN, 101000, "one hundred one thousand € 0 ct.", Label = "in english, 101000 euro reads as one hundred one thousand")]
    [InlineData(Language.EN, 102000, "one hundred two thousand € 0 ct.", Label = "in english, 102000 euro reads as one hundred two thousand")]
    [InlineData(Language.EN, 110000, "one hundred ten thousand € 0 ct.", Label = "in english, 110000 euro reads as one hundred ten thousand")]
    [InlineData(Language.EN, 111000, "one hundred eleven thousand € 0 ct.", Label = "in english, 111000 euro reads as one hundred eleven thousand")]
    [InlineData(Language.EN, 120000, "one hundred twenty thousand € 0 ct.", Label = "in english, 120000 euro reads as one hundred twenty thousand")]
    [InlineData(Language.EN, 121000, "one hundred twenty one thousand € 0 ct.", Label = "in english, 121000 euro reads as one hundred twenty one thousand")]
    [InlineData(Language.EN, 122000, "one hundred twenty two thousand € 0 ct.", Label = "in english, 122000 euro reads as one hundred twenty two thousand")]
    [InlineData(Language.EN, 122001, "one hundred twenty two thousand one € 0 ct.", Label = "in english, 122001 euro reads as one hundred twenty two thousand one")]
    [InlineData(Language.EN, 999999, "nine hundred ninety nine thousand nine hundred ninety nine € 0 ct.", Label = "in english, 999999 euro reads as nine hundred ninety nine thousand nine hundred ninety nine")]
    [InlineData(Language.EN, 1000000, "one million € 0 ct.", Label = "in english, 1000000 euro reads as one million")]
    [InlineData(Language.EN, 2000000, "two million € 0 ct.", Label = "in english, 2000000 euro reads as two million")]
    [InlineData(Language.EN, 10000000, "ten million € 0 ct.", Label = "in english, 10000000 euro reads as ten million")]
    [InlineData(Language.EN, 11000000, "eleven million € 0 ct.", Label = "in english, 11000000 euro reads as eleven million")]
    [InlineData(Language.EN, 20000000, "twenty million € 0 ct.", Label = "in english, 20000000 euro reads as twenty million")]
    [InlineData(Language.EN, 21000000, "twenty one million € 0 ct.", Label = "in english, 21000000 euro reads as twenty one million")]
    [InlineData(Language.EN, 22000000, "twenty two million € 0 ct.", Label = "in english, 22000000 euro reads as twenty two million")]
    [InlineData(Language.EN, 30000000, "thirty million € 0 ct.", Label = "in english, 30000000 euro reads as thirty million")]
    [InlineData(Language.EN, 31000000, "thirty one million € 0 ct.", Label = "in english, 31000000 euro reads as thirty one million")]
    [InlineData(Language.EN, 32000000, "thirty two million € 0 ct.", Label = "in english, 32000000 euro reads as thirty two million")]
    [InlineData(Language.EN, 100000000, "one hundred million € 0 ct.", Label = "in english, 100000000 euro reads as one hundred million")]
    [InlineData(Language.EN, 101000000, "one hundred one million € 0 ct.", Label = "in english, 101000000 euro reads as one hundred one million")]
    [InlineData(Language.EN, 102000000, "one hundred two million € 0 ct.", Label = "in english, 102000000 euro reads as one hundred two million")]
    [InlineData(Language.EN, 110000000, "one hundred ten million € 0 ct.", Label = "in english, 110000000 euro reads as one hundred ten million")]
    [InlineData(Language.EN, 111000000, "one hundred eleven million € 0 ct.", Label = "in english, 111000000 euro reads as one hundred eleven million")]
    [InlineData(Language.EN, 120000000, "one hundred twenty million € 0 ct.", Label = "in english, 120000000 euro reads as one hundred twenty million")]
    [InlineData(Language.EN, 121000000, "one hundred twenty one million € 0 ct.", Label = "in english, 121000000 euro reads as one hundred twenty one million")]
    [InlineData(Language.EN, 122000000, "one hundred twenty two million € 0 ct.", Label = "in english, 122000000 euro reads as one hundred twenty two million")]
    [InlineData(Language.EN, 122000001, "one hundred twenty two million one € 0 ct.", Label = "in english, 122000001 euro reads as one hundred twenty two million one")]
    [InlineData(Language.LT, 0, "nulis € 0 ct.", Label = "in lithuanian, zero price reads as zero euro and zero cents")]
    [InlineData(Language.LT, 0.10, "nulis € 10 ct.", Label = "in lithuanian, 10 cents with zero euro")]
    [InlineData(Language.LT, 0.11, "nulis € 11 ct.", Label = "in lithuanian, 11 cents with zero euro")]
    [InlineData(Language.LT, 0.111111, "nulis € 11 ct.", Label = "in lithuanian, cents beyond two digits are truncated")]
    [InlineData(Language.LT, 1, "vienas € 0 ct.", Label = "in lithuanian, 1 euro reads as vienas")]
    [InlineData(Language.LT, 2, "du € 0 ct.", Label = "in lithuanian, 2 euro reads as du")]
    [InlineData(Language.LT, 3, "trys € 0 ct.", Label = "in lithuanian, 3 euro reads as trys")]
    [InlineData(Language.LT, 4, "keturi € 0 ct.", Label = "in lithuanian, 4 euro reads as keturi")]
    [InlineData(Language.LT, 5, "penki € 0 ct.", Label = "in lithuanian, 5 euro reads as penki")]
    [InlineData(Language.LT, 6, "šeši € 0 ct.", Label = "in lithuanian, 6 euro reads as šeši")]
    [InlineData(Language.LT, 7, "septyni € 0 ct.", Label = "in lithuanian, 7 euro reads as septyni")]
    [InlineData(Language.LT, 8, "aštuoni € 0 ct.", Label = "in lithuanian, 8 euro reads as aštuoni")]
    [InlineData(Language.LT, 9, "devyni € 0 ct.", Label = "in lithuanian, 9 euro reads as devyni")]
    [InlineData(Language.LT, 10, "dešimt € 0 ct.", Label = "in lithuanian, 10 euro reads as dešimt")]
    [InlineData(Language.LT, 11, "vienuolika € 0 ct.", Label = "in lithuanian, 11 euro reads as vienuolika")]
    [InlineData(Language.LT, 12, "dvylika € 0 ct.", Label = "in lithuanian, 12 euro reads as dvylika")]
    [InlineData(Language.LT, 13, "trylika € 0 ct.", Label = "in lithuanian, 13 euro reads as trylika")]
    [InlineData(Language.LT, 14, "keturiolika € 0 ct.", Label = "in lithuanian, 14 euro reads as keturiolika")]
    [InlineData(Language.LT, 15, "penkiolika € 0 ct.", Label = "in lithuanian, 15 euro reads as penkiolika")]
    [InlineData(Language.LT, 16, "šešiolika € 0 ct.", Label = "in lithuanian, 16 euro reads as šešiolika")]
    [InlineData(Language.LT, 17, "septyniolika € 0 ct.", Label = "in lithuanian, 17 euro reads as septyniolika")]
    [InlineData(Language.LT, 18, "aštuoniolika € 0 ct.", Label = "in lithuanian, 18 euro reads as aštuoniolika")]
    [InlineData(Language.LT, 19, "devyniolika € 0 ct.", Label = "in lithuanian, 19 euro reads as devyniolika")]
    [InlineData(Language.LT, 20, "dvidešimt € 0 ct.", Label = "in lithuanian, 20 euro reads as dvidešimt")]
    [InlineData(Language.LT, 30, "trisdešimt € 0 ct.", Label = "in lithuanian, 30 euro reads as trisdešimt")]
    [InlineData(Language.LT, 40, "keturiasdešimt € 0 ct.", Label = "in lithuanian, 40 euro reads as keturiasdešimt")]
    [InlineData(Language.LT, 41, "keturiasdešimt vienas € 0 ct.", Label = "in lithuanian, 41 euro reads as keturiasdešimt vienas")]
    [InlineData(Language.LT, 99, "devyniasdešimt devyni € 0 ct.", Label = "in lithuanian, 99 euro reads as devyniasdešimt devyni")]
    [InlineData(Language.LT, 100, "vienas šimtas € 0 ct.", Label = "in lithuanian, 100 euro reads as vienas šimtas")]
    [InlineData(Language.LT, 200, "du šimtai € 0 ct.", Label = "in lithuanian, 200 euro reads as du šimtai")]
    [InlineData(Language.LT, 210, "du šimtai dešimt € 0 ct.", Label = "in lithuanian, 210 euro reads as du šimtai dešimt")]
    [InlineData(Language.LT, 1000, "vienas tūkstantis € 0 ct.", Label = "in lithuanian, 1000 euro reads as vienas tūkstantis")]
    [InlineData(Language.LT, 2000, "du tūkstančiai € 0 ct.", Label = "in lithuanian, 2000 euro reads as du tūkstančiai")]
    [InlineData(Language.LT, 10000, "dešimt tūkstančių € 0 ct.", Label = "in lithuanian, 10000 euro reads as dešimt tūkstančių")]
    [InlineData(Language.LT, 11000, "vienuolika tūkstančių € 0 ct.", Label = "in lithuanian, 11000 euro reads as vienuolika tūkstančių")]
    [InlineData(Language.LT, 20000, "dvidešimt tūkstančių € 0 ct.", Label = "in lithuanian, 20000 euro reads as dvidešimt tūkstančių")]
    [InlineData(Language.LT, 21000, "dvidešimt vienas tūkstantis € 0 ct.", Label = "in lithuanian, 21000 euro reads as dvidešimt vienas tūkstantis")]
    [InlineData(Language.LT, 22000, "dvidešimt du tūkstančiai € 0 ct.", Label = "in lithuanian, 22000 euro reads as dvidešimt du tūkstančiai")]
    [InlineData(Language.LT, 30000, "trisdešimt tūkstančių € 0 ct.", Label = "in lithuanian, 30000 euro reads as trisdešimt tūkstančių")]
    [InlineData(Language.LT, 31000, "trisdešimt vienas tūkstantis € 0 ct.", Label = "in lithuanian, 31000 euro reads as trisdešimt vienas tūkstantis")]
    [InlineData(Language.LT, 32000, "trisdešimt du tūkstančiai € 0 ct.", Label = "in lithuanian, 32000 euro reads as trisdešimt du tūkstančiai")]
    [InlineData(Language.LT, 100000, "vienas šimtas tūkstančių € 0 ct.", Label = "in lithuanian, 100000 euro reads as vienas šimtas tūkstančių")]
    [InlineData(Language.LT, 101000, "vienas šimtas vienas tūkstantis € 0 ct.", Label = "in lithuanian, 101000 euro reads as vienas šimtas vienas tūkstantis")]
    [InlineData(Language.LT, 102000, "vienas šimtas du tūkstančiai € 0 ct.", Label = "in lithuanian, 102000 euro reads as vienas šimtas du tūkstančiai")]
    [InlineData(Language.LT, 110000, "vienas šimtas dešimt tūkstančių € 0 ct.", Label = "in lithuanian, 110000 euro reads as vienas šimtas dešimt tūkstančių")]
    [InlineData(Language.LT, 111000, "vienas šimtas vienuolika tūkstančių € 0 ct.", Label = "in lithuanian, 111000 euro reads as vienas šimtas vienuolika tūkstančių")]
    [InlineData(Language.LT, 120000, "vienas šimtas dvidešimt tūkstančių € 0 ct.", Label = "in lithuanian, 120000 euro reads as vienas šimtas dvidešimt tūkstančių")]
    [InlineData(Language.LT, 121000, "vienas šimtas dvidešimt vienas tūkstantis € 0 ct.", Label = "in lithuanian, 121000 euro reads as vienas šimtas dvidešimt vienas tūkstantis")]
    [InlineData(Language.LT, 122000, "vienas šimtas dvidešimt du tūkstančiai € 0 ct.", Label = "in lithuanian, 122000 euro reads as vienas šimtas dvidešimt du tūkstančiai")]
    [InlineData(Language.LT, 122001, "vienas šimtas dvidešimt du tūkstančiai vienas € 0 ct.", Label = "in lithuanian, 122001 euro reads as vienas šimtas dvidešimt du tūkstančiai vienas")]
    [InlineData(Language.LT, 999999, "devyni šimtai devyniasdešimt devyni tūkstančiai devyni šimtai devyniasdešimt devyni € 0 ct.", Label = "in lithuanian, 999999 euro reads as devyni šimtai devyniasdešimt devyni tūkstančiai devyni šimtai devyniasdešimt devyni")]
    [InlineData(Language.LT, 1000000, "vienas milijonas € 0 ct.", Label = "in lithuanian, 1000000 euro reads as vienas milijonas")]
    [InlineData(Language.LT, 2000000, "du milijonai € 0 ct.", Label = "in lithuanian, 2000000 euro reads as du milijonai")]
    [InlineData(Language.LT, 10000000, "dešimt milijonų € 0 ct.", Label = "in lithuanian, 10000000 euro reads as dešimt milijonų")]
    [InlineData(Language.LT, 11000000, "vienuolika milijonų € 0 ct.", Label = "in lithuanian, 11000000 euro reads as vienuolika milijonų")]
    [InlineData(Language.LT, 20000000, "dvidešimt milijonų € 0 ct.", Label = "in lithuanian, 20000000 euro reads as dvidešimt milijonų")]
    [InlineData(Language.LT, 21000000, "dvidešimt vienas milijonas € 0 ct.", Label = "in lithuanian, 21000000 euro reads as dvidešimt vienas milijonas")]
    [InlineData(Language.LT, 22000000, "dvidešimt du milijonai € 0 ct.", Label = "in lithuanian, 22000000 euro reads as dvidešimt du milijonai")]
    [InlineData(Language.LT, 30000000, "trisdešimt milijonų € 0 ct.", Label = "in lithuanian, 30000000 euro reads as trisdešimt milijonų")]
    [InlineData(Language.LT, 31000000, "trisdešimt vienas milijonas € 0 ct.", Label = "in lithuanian, 31000000 euro reads as trisdešimt vienas milijonas")]
    [InlineData(Language.LT, 32000000, "trisdešimt du milijonai € 0 ct.", Label = "in lithuanian, 32000000 euro reads as trisdešimt du milijonai")]
    [InlineData(Language.LT, 100000000, "vienas šimtas milijonų € 0 ct.", Label = "in lithuanian, 100000000 euro reads as vienas šimtas milijonų")]
    [InlineData(Language.LT, 101000000, "vienas šimtas vienas milijonas € 0 ct.", Label = "in lithuanian, 101000000 euro reads as vienas šimtas vienas milijonas")]
    [InlineData(Language.LT, 102000000, "vienas šimtas du milijonai € 0 ct.", Label = "in lithuanian, 102000000 euro reads as vienas šimtas du milijonai")]
    [InlineData(Language.LT, 110000000, "vienas šimtas dešimt milijonų € 0 ct.", Label = "in lithuanian, 110000000 euro reads as vienas šimtas dešimt milijonų")]
    [InlineData(Language.LT, 111000000, "vienas šimtas vienuolika milijonų € 0 ct.", Label = "in lithuanian, 111000000 euro reads as vienas šimtas vienuolika milijonų")]
    [InlineData(Language.LT, 120000000, "vienas šimtas dvidešimt milijonų € 0 ct.", Label = "in lithuanian, 120000000 euro reads as vienas šimtas dvidešimt milijonų")]
    [InlineData(Language.LT, 121000000, "vienas šimtas dvidešimt vienas milijonas € 0 ct.", Label = "in lithuanian, 121000000 euro reads as vienas šimtas dvidešimt vienas milijonas")]
    [InlineData(Language.LT, 122000000, "vienas šimtas dvidešimt du milijonai € 0 ct.", Label = "in lithuanian, 122000000 euro reads as vienas šimtas dvidešimt du milijonai")]
    [InlineData(Language.LT, 122000001, "vienas šimtas dvidešimt du milijonai vienas € 0 ct.", Label = "in lithuanian, 122000001 euro reads as vienas šimtas dvidešimt du milijonai vienas")]
    public void GetConverter_GivenLanguage_DecodesPrice(Language language, decimal number, string expected)
    {
        //Arrange
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(language);

        //Act
        string result = priceToWords.Decode(number);

        //Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(Language.EN, -1, Label = "in english, negative price throws")]
    [InlineData(Language.EN, 1000000001, Label = "in english, price above one billion throws")]
    [InlineData(Language.LT, -1, Label = "in lithuanian, negative price throws")]
    [InlineData(Language.LT, 1000000001, Label = "in lithuanian, price above one billion throws")]
    public void GetConverter_GivenLanguageAndInvalidPrice_DecodeThrows(Language language, decimal number)
    {
        //Arrange
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(language);

        //Act
        Action act = () => priceToWords.Decode(number);

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}
