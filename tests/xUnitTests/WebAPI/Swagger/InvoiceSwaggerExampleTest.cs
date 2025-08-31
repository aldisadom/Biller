using FluentAssertions;
using WebAPI.SwaggerExamples.Invoice;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class InvoiceSwaggerExampleTest
{

    [Fact]
    public void InvoiceDataAddRequestExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceAddRequestExample example = new();
        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void InvoiceDataUpdateRequestExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceUpdateRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void InvoiceDataListResponseExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceListResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void InvoiceDataResponseExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
