using FluentAssertions;
using WebAPI.SwaggerExamples.User;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class UserSwaggerExampleTest
{
    public static IEnumerable<TheoryDataRow<Func<object>>> GetExampleTestData()
    {
        yield return new(() => new UserAddRequestExample().GetExamples()) { Label = "user add request example has no empty properties" };
        yield return new(() => new UserUpdateRequestExample().GetExamples()) { Label = "user update request example has no empty properties" };
        yield return new(() => new UserListResponseExample().GetExamples()) { Label = "user list response example has no empty properties" };
        yield return new(() => new UserResponseExample().GetExamples()) { Label = "user response example has no empty properties" };
        yield return new(() => new UserLoginRequestExample().GetExamples()) { Label = "user login request example has no empty properties" };
        yield return new(() => new UserLoginResponseExample().GetExamples()) { Label = "user login response example has no empty properties" };
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
