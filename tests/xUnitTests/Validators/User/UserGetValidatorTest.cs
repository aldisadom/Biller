using BillerContracts.Requests.User;
using FluentAssertions;
using Validators.User;

namespace xUnitTests.Validators.User;

public class UserGetValidatorTest
{
    private readonly UserGetValidator _validator = new();

    [Fact]
    public void Validate_GivenDefaultRequest_IsValid()
    {
        //Arrange
        var request = new UserGetRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenPageZero_AppliesPagingRules()
    {
        //Arrange
        var request = new UserGetRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
    }
}
