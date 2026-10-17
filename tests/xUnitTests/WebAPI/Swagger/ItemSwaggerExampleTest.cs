using FluentAssertions;
using WebAPI.SwaggerExamples.Item;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class ItemSwaggerExampleTest
{
    [Fact]
    public void GetExamples_GivenItemAddRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenItemUpdateRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenItemListResponseExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        ItemListResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenItemResponseExample_HasNoNullOrEmptyProperties()
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
