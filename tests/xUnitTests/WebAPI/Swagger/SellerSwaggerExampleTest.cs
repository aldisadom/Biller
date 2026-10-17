using FluentAssertions;
using WebAPI.SwaggerExamples.Seller;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class SellerSwaggerExampleTest
{

    [Fact]
    public void GetExamples_GivenSellerAddRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenSellerUpdateRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenSellerListResponseExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenSellerResponseExample_HasNoNullOrEmptyProperties()
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
