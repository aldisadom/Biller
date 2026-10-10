using Application.Models;
using FluentAssertions;

namespace xUnitTests.Application.Models;

public class InvoiceItemModelTest
{
    [Theory]
    [InlineData(10, 3, 30)]
    [InlineData(2.5, 1.5, 3.75)]
    [InlineData(0, 5, 0)]
    [InlineData(99.99, 0, 0)]
    public void CalculateTotal_GivenPriceAndQuantity_ReturnsProduct(decimal price, decimal quantity, decimal expected)
    {
        //Arrange
        var item = new InvoiceItemModel { Price = price, Quantity = quantity };

        //Act
        decimal result = item.CalculateTotal();

        //Assert
        result.Should().Be(expected);
    }
}
