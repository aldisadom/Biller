using BillerContracts.Requests.User;
using FluentAssertions;
using Validators;
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
        _validator.Should().BeAssignableTo<PagedRequestValidator<UserGetRequest>>();
    }
}
