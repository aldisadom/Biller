using BillerContracts.Requests.Item;
using FluentAssertions;
using Validators.Item;

namespace xUnitTests.Validators.Item;

public class ItemGetValidatorTest
{
    private readonly ItemGetValidator _validator = new();

    [Fact]
    public void Validate_GivenDefaultRequest_IsValid()
    {
        //Arrange
        var request = new ItemGetRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenCustomerId_IsValid()
    {
        //Arrange
        var request = new ItemGetRequest { CustomerId = Guid.NewGuid() };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenEmptyCustomerId_ReturnsCustomerIdMessage()
    {
        //Arrange
        var request = new ItemGetRequest { CustomerId = Guid.Empty };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.CustomerId))
            .Which.ErrorMessage.Should().Be("Please specify valid customer Id");
    }

    [Fact]
    public void Validate_GivenPageZero_AppliesPagingRules()
    {
        //Arrange
        var request = new ItemGetRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
    }
}
