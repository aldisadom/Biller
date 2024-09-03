using FluentAssertions;
using WebAPI.SwaggerExamples.InvoiceData;
using WebAPI.SwaggerExamples.Item;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class ItemSwaggerExampleTest
{
    [Fact]
    public void ItemAddRequestExample_ReturnEmptyList()
    {
        //Arrange
        ItemAddRequestExample example = new();
        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void ItemUpdateRequestExample_ReturnEmptyList()
    {
        //Arrange
        ItemUpdateRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void ItemListResponseExample_ReturnEmptyList()
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
    public void ItemResponseExample_ReturnEmptyList()
    {
        //Arrange
        ItemResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
