using FluentAssertions;
using WebAPI.SwaggerExamples.Invoice;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class InvoiceSwaggerExampleTest
{

    [Fact]
    public void InvoiceAddRequestExample_ReturnEmptyList()
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
    public void InvoiceUpdateRequestExample_ReturnEmptyList()
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
    public void InvoiceListResponseExample_ReturnEmptyList()
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
    public void InvoiceResponseExample_ReturnEmptyList()
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
