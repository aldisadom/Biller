using Application.Models;
using FluentAssertions;

namespace xUnitTests.Application.Models;

public class InvoiceModelTest
{
    [Theory]
    [InlineData(1, "000001", Label = "one digit number is padded to six digits")]
    [InlineData(42, "000042", Label = "two digit number is padded to six digits")]
    [InlineData(123456, "123456", Label = "six digit number is not padded")]
    [InlineData(1234567, "1234567", Label = "seven digit number is kept whole")]
    public void GenerateInvoiceName_GivenInvoiceNumber_ReturnsPaddedNumber(int invoiceNumber, string expected)
    {
        //Arrange
        InvoiceModel invoice = new() { InvoiceNumber = invoiceNumber };

        //Act
        string result = invoice.GenerateInvoiceName();

        //Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateFileLocation_GivenSeriesAndNumber_UsesInvoiceSeriesName()
    {
        //Arrange
        InvoiceModel invoice = new()
        {
            SeriesName = "WF",
            InvoiceNumber = 12
        };

        //Act
        string result = invoice.GenerateFileLocation();

        //Assert
        result.Should().Be("WF-000012.pdf");
    }

    [Fact]
    public void CalculateTotal_GivenItems_ReturnsSumOfPriceTimesQuantity()
    {
        //Arrange
        InvoiceModel invoice = new()
        {
            Items =
            [
                new() { Price = 10.5m, Quantity = 2 },
                new() { Price = 3m, Quantity = 1.5m }
            ]
        };

        //Act
        decimal result = invoice.CalculateTotal();

        //Assert
        result.Should().Be(25.5m);
    }
}
