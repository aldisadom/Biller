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
        priceToWords.GetType().Should().Be(typeof(global::Application.Helpers.PriceToWords.PriceToWords));
    }

    [Fact]
    public void GetConverter_GivenUnsupportedLanguage_ThrowsNotSupportedException()
    {
        //Arrange
        //Act
        //Assert
        try
        {
            IPriceToWords priceToWords = _priceToWordsFactory.GetConverter((Language)999);
            Assert.Fail();
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }
}
