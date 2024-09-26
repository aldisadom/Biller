using FluentAssertions;
using WebAPI.SwaggerExamples.User;
using xUnitTests.Helpers;

namespace xUnitTests.WebAPI.Swagger;

public class UserSwaggerExampleTest
{

    [Fact]
    public void UserAddRequestExample_ReturnEmptyList()
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
    public void UserUpdateRequestExample_ReturnEmptyList()
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
    public void UserListResponseExample_ReturnEmptyList()
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
    public void UserResponseExample_ReturnEmptyList()
    {
        //Arrange
        UserResponseExample example = new();

        //Act
        var exampleValues = example.GetExamples();
        List<string> nullProperties = NullChecker.GetNullOrEmptyProperties(exampleValues);

        //Assert
        nullProperties.Should().BeEmpty();
    }
}
