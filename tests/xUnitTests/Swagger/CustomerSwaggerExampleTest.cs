using FluentAssertions;
using WebAPI.SwaggerExamples.Customer;
using xUnitTests.Helpers;

namespace xUnitTests.Swagger;

public class CustomerSwaggerExampleTest
{
    [Fact]
    public void CustomerAddRequestExample_ReturnEmptyList()
    {
        //Arrange
        CustomerAddRequestExample example = new();
        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void CustomerUpdateRequestExample_ReturnEmptyList()
    {
        //Arrange
        CustomerUpdateRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void CustomerListResponseExample_ReturnEmptyList()
    {
        //Arrange
        CustomerListResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void CustomerResponseExample_ReturnEmptyList()
    {
        //Arrange
        CustomerResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
