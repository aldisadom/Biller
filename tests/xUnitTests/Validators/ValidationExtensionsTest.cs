using AwesomeAssertions;
using BillerContracts.Requests.Item;
using FluentValidation;
using Validators;
using Validators.Item;

namespace xUnitTests.Validators;

public class ValidationExtensionsTest
{
    private readonly ItemAddValidator _validator = new(RepoFonts.CalibriChecker);

    [Fact]
    public void CheckValidation_GivenNoRequest_ThrowsNotProvided()
    {
        //Act
        Action act = () => _validator.CheckValidation(null);

        //Assert
        act.Should().Throw<ValidationException>()
            .WithMessage($"{typeof(ItemAddRequest)} is not provided");
    }
}
