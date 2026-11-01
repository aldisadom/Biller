using BillerContracts.Enums;
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

    [Theory]
    [InlineData(1, true, Label = "first page is accepted")]
    [InlineData(42949672, true, Label = "highest page whose offset fits in an int is accepted")]
    [InlineData(0, false, Label = "page 0 is rejected")]
    [InlineData(-1, false, Label = "negative page is rejected")]
    [InlineData(42949673, false, Label = "page whose offset overflows an int is rejected")]
    public void Validate_GivenPage_ReturnsExpected(int page, bool expected)
    {
        //Arrange
        var request = new CustomerGetRequest { Page = page, PageSize = PageSize.p50 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void Validate_GivenPageZero_ReturnsPageRangeMessage()
    {
        //Arrange
        var request = new CustomerGetRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
    }

    [Fact]
    public void Validate_GivenUndefinedPageSize_IsInvalid()
    {
        //Arrange
        var request = new CustomerGetRequest { PageSize = (PageSize)7 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.PageSize));
    }
}
