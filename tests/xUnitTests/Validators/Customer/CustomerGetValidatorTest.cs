using BillerContracts.Requests.Customer;
using FluentAssertions;
using Validators.Customer;

namespace xUnitTests.Validators.Customer;

public class CustomerGetValidatorTest
{
    private readonly CustomerGetValidator _validator = new();

    [Fact]
    public void Validate_GivenDefaultRequest_IsValid()
    {
        //Arrange
        var request = new CustomerGetRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenSellerId_IsValid()
    {
        //Arrange
        var request = new CustomerGetRequest { SellerId = Guid.NewGuid() };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenEmptySellerId_ReturnsSellerIdMessage()
    {
        //Arrange
        var request = new CustomerGetRequest { SellerId = Guid.Empty };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.SellerId))
            .Which.ErrorMessage.Should().Be("Please specify valid seller Id");
    }

    [Fact]
    public void Validate_GivenPageZero_AppliesPagingRules()
    {
        //Arrange
        var request = new CustomerGetRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
    }
}
