using FluentAssertions;
using WebAPI.SwaggerExamples.InvoiceData;
using xUnitTests.Helpers;

namespace xUnitTests.Swagger;

public class InvoiceSwaggerExampleTest
{

    [Fact]
    public void InvoiceDataGenerateRequestExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceDataGenerateRequestExample example = new();
        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void InvoiceDataGenerateResponseExample_ReturnEmptyList()
    {
        //Arrange
        InvoiceDataGenerateResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
