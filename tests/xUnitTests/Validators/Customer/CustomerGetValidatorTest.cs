using BillerContracts.Requests.Customer;
using AwesomeAssertions;
using Validators;
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
        _validator.Should().BeAssignableTo<PagedRequestValidator<CustomerGetRequest>>();
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
}
