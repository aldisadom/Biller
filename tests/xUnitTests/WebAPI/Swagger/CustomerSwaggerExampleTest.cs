using FluentAssertions;
using WebAPI.SwaggerExamples.Customer;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class CustomerSwaggerExampleTest
{
    public static IEnumerable<TheoryDataRow<Func<object>>> GetExampleTestData()
    {
        yield return new(() => new CustomerAddRequestExample().GetExamples()) { Label = "customer add request example has no empty properties" };
        yield return new(() => new CustomerUpdateRequestExample().GetExamples()) { Label = "customer update request example has no empty properties" };
        yield return new(() => new CustomerListResponseExample().GetExamples()) { Label = "customer list response example has no empty properties" };
        yield return new(() => new CustomerResponseExample().GetExamples()) { Label = "customer response example has no empty properties" };
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
