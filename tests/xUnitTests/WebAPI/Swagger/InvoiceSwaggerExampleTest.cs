using FluentAssertions;
using WebAPI.SwaggerExamples.InvoiceData;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class InvoiceSwaggerExampleTest
{

    [Fact]
    public void InvoiceDataAddRequestExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceDataAddRequestExample example = new();
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
        InvoiceDataUpdateRequestExample example = new();

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
        InvoiceDataListResponseExample example = new();

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
        InvoiceDataResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
