using FluentAssertions;
using WebAPI.SwaggerExamples.Seller;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class SellerSwaggerExampleTest
{

    [Fact]
    public void SellerAddRequestExample_ReturnEmptyList()
    {
        //Arrange
        SellerAddRequestExample example = new();
        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void SellerUpdateRequestExample_ReturnEmptyList()
    {
        //Arrange
        SellerUpdateRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void SellerListResponseExample_ReturnEmptyList()
    {
        //Arrange
        SellerListResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void SellerResponseExample_ReturnEmptyList()
    {
        //Arrange
        SellerResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
