using FluentAssertions;
using WebAPI.SwaggerExamples.Series;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class SeriesSwaggerExampleTest
{
    public static IEnumerable<TheoryDataRow<Func<object>>> GetExampleTestData()
    {
        yield return new(() => new SeriesAddRequestExample().GetExamples()) { Label = "series add request example has no empty properties" };
        yield return new(() => new SeriesUpdateRequestExample().GetExamples()) { Label = "series update request example has no empty properties" };
        yield return new(() => new SeriesListResponseExample().GetExamples()) { Label = "series list response example has no empty properties" };
        yield return new(() => new SeriesResponseExample().GetExamples()) { Label = "series response example has no empty properties" };
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
