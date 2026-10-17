using FluentAssertions;
using WebAPI.SwaggerExamples.Seller;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class SellerSwaggerExampleTest
{
    public static IEnumerable<TheoryDataRow<Func<object>>> GetExampleTestData()
    {
        yield return new(() => new SellerAddRequestExample().GetExamples()) { Label = "seller add request example has no empty properties" };
        yield return new(() => new SellerUpdateRequestExample().GetExamples()) { Label = "seller update request example has no empty properties" };
        yield return new(() => new SellerListResponseExample().GetExamples()) { Label = "seller list response example has no empty properties" };
        yield return new(() => new SellerResponseExample().GetExamples()) { Label = "seller response example has no empty properties" };
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
