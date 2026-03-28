using Application.Helpers.PriceToWords;
using Contracts.Enums;
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
    [InlineData(Language.EN)]
    [InlineData(Language.LT)]
    public void GetConverter_Returns(Language language)
    {
        //Arrange
        //Act
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(language);

        //Assert
        priceToWords.GetType().Should().Be(typeof(global::Application.Helpers.PriceToWords.PriceToWords));
    }

    [Fact]
    public void GetConverter_Invalid_Throws()
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
