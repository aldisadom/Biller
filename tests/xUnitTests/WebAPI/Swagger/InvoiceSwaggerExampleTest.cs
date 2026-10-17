using FluentAssertions;
using WebAPI.SwaggerExamples.Invoice;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class InvoiceSwaggerExampleTest
{
    public static IEnumerable<TheoryDataRow<Func<object>>> GetExampleTestData()
    {
        yield return new(() => new InvoiceAddRequestExample().GetExamples()) { Label = "invoice add request example has no empty properties" };
        yield return new(() => new InvoiceUpdateRequestExample().GetExamples()) { Label = "invoice update request example has no empty properties" };
        yield return new(() => new InvoiceListResponseExample().GetExamples()) { Label = "invoice list response example has no empty properties" };
        yield return new(() => new InvoiceResponseExample().GetExamples()) { Label = "invoice response example has no empty properties" };
        yield return new(() => new InvoiceUpdateStatusRequestExample().GetExamples()) { Label = "invoice update status request example has no empty properties" };
    }

    [Theory]
    [MemberData(nameof(GetExampleTestData))]
    public void GetExamples_GivenExample_HasNoNullOrEmptyProperties(Func<object> getExamples)
    {
        //Arrange
        //Act
        var exampleValues = getExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
