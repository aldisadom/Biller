using FluentAssertions;
using WebAPI.SwaggerExamples.Invoice;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class InvoiceSwaggerExampleTest
{

    [Fact]
    public void GetExamples_GivenInvoiceAddRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenInvoiceUpdateRequestExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenInvoiceListResponseExample_HasNoNullOrEmptyProperties()
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
    public void GetExamples_GivenInvoiceResponseExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        InvoiceResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenInvoiceUpdateStatusRequestExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        InvoiceUpdateStatusRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
