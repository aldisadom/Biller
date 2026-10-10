using Application.Models;
using FluentAssertions;

namespace xUnitTests.Application.Models;

public class InvoiceModelTest
{
    [Theory]
    [InlineData(1, "000001")]
    [InlineData(9, "000009")]
    [InlineData(10, "000010")]
    [InlineData(99, "000099")]
    [InlineData(100, "000100")]
    [InlineData(999, "000999")]
    [InlineData(1000, "001000")]
    [InlineData(9999, "009999")]
    [InlineData(10000, "010000")]
    [InlineData(99999, "099999")]
    [InlineData(100000, "100000")]
    [InlineData(1234567, "1234567")]
    public void GenerateInvoiceName_GivenInvoiceNumber_PadsToSixDigits(int invoiceNumber, string expected)
    {
        //Arrange
        var invoice = new InvoiceModel { Customer = new CustomerModel { InvoiceNumber = invoiceNumber } };

        //Act
        string result = invoice.GenerateInvoiceName();

        //Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateFileLocation_GivenCustomer_ReturnsPdfName()
    {
        //Arrange
        var invoice = new InvoiceModel { Customer = new CustomerModel { InvoiceName = "ACME", InvoiceNumber = 42 } };

        //Act
        string result = invoice.GenerateFileLocation();

        //Assert
        result.Should().Be("ACME-000042.pdf");
    }

    [Fact]
    public void CalculateTotal_GivenItems_ReturnsSumOfLineTotals()
    {
        //Arrange
        var invoice = new InvoiceModel
        {
            Items =
            [
                new InvoiceItemModel { Price = 10, Quantity = 2 },
                new InvoiceItemModel { Price = 2.5m, Quantity = 4 },
            ]
        };

        //Act
        decimal result = invoice.CalculateTotal();

        //Assert
        result.Should().Be(30m);
    }

    [Fact]
    public void CalculateTotal_GivenNoItems_ReturnsZero()
    {
        //Arrange
        var invoice = new InvoiceModel { Items = [] };

        //Act
        decimal result = invoice.CalculateTotal();

        //Assert
        result.Should().Be(0m);
    }
}
