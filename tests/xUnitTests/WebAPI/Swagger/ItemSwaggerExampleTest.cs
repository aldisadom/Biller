using FluentAssertions;
using WebAPI.SwaggerExamples.Item;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class ItemSwaggerExampleTest
{
    public static IEnumerable<TheoryDataRow<Func<object>>> GetExampleTestData()
    {
        yield return new(() => new ItemAddRequestExample().GetExamples()) { Label = "item add request example has no empty properties" };
        yield return new(() => new ItemUpdateRequestExample().GetExamples()) { Label = "item update request example has no empty properties" };
        yield return new(() => new ItemListResponseExample().GetExamples()) { Label = "item list response example has no empty properties" };
        yield return new(() => new ItemResponseExample().GetExamples()) { Label = "item response example has no empty properties" };
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
