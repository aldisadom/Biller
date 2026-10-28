using Application.Models;
using FluentAssertions;

namespace xUnitTests.Application.Models;

public class InvoiceModelTest
{
    [Theory]
    [InlineData(5, "000005", Label = "one digit number is padded to six digits")]
    [InlineData(123, "000123", Label = "three digit number is padded to six digits")]
    [InlineData(123456, "123456", Label = "six digit number is not padded")]
    [InlineData(1234567, "1234567", Label = "number longer than six digits is kept whole")]
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
    public void GenerateFileLocation_GivenInvoiceName_ReturnsNameAndPaddedNumber()
    {
        //Arrange
        InvoiceModel invoice = new() { InvoiceNumber = 5 };

        //Act
        string result = invoice.GenerateFileLocation("WF");

        //Assert
        result.Should().Be("WF-000005.pdf");
    }
}
