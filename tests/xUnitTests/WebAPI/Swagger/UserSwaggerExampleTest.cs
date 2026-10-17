using FluentAssertions;
using WebAPI.SwaggerExamples.User;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class UserSwaggerExampleTest
{

    [Fact]
    public void GetExamples_GivenUserAddRequestExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        UserAddRequestExample example = new();
        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenUserUpdateRequestExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        UserUpdateRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenUserListResponseExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        UserListResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenUserResponseExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        UserResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenUserLoginRequestExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        UserLoginRequestExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }

    [Fact]
    public void GetExamples_GivenUserLoginResponseExample_HasNoNullOrEmptyProperties()
    {
        //Arrange
        UserLoginResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
