using FluentAssertions;
using WebAPI.SwaggerExamples.Customer;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class CustomerSwaggerExampleTest
{
    [Fact]
    public void GetExamples_GivenCustomerAddRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenCustomerUpdateRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenCustomerListResponseExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenCustomerResponseExample_HasNoNullOrEmptyProperties()
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
