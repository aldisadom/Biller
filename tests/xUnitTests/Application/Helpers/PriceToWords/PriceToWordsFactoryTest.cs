using Application.Helpers.PriceToWords;
using Common.Enums;
using FluentAssertions;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsFactoryTest
{
    private readonly PriceToWordsFactory _priceToWordsFactory;

    public PriceToWordsFactoryTest()
    {
        _priceToWordsFactory = new();
    }

    [Fact]
    public void GetConverter_LT_ReturnsLT()
    {
        //Arrange
        //Act
        IPriceToWords priceToWords = _priceToWordsFactory.GetConverter(Language.LT);

        //Assert
        priceToWords.GetType().Should().Be(typeof(PriceToWordsLT));
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
