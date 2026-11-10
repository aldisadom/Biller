using BillerContracts.Requests.Seller;
using AwesomeAssertions;
using Validators;
using Validators.Seller;

namespace xUnitTests.Validators.Seller;

public class SellerGetValidatorTest
{
    private readonly SellerGetValidator _validator = new();

    [Fact]
    public void Validate_GivenDefaultRequest_IsValid()
    {
        //Arrange
        var request = new SellerGetRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
        _validator.Should().BeAssignableTo<PagedRequestValidator<SellerGetRequest>>();
    }

    [Fact]
    public void Validate_GivenUserId_IsValid()
    {
        //Arrange
        var request = new SellerGetRequest { UserId = Guid.NewGuid() };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenEmptyUserId_ReturnsUserIdMessage()
    {
        //Arrange
        var request = new SellerGetRequest { UserId = Guid.Empty };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.UserId))
            .Which.ErrorMessage.Should().Be("Please specify valid user Id");
    }
}
