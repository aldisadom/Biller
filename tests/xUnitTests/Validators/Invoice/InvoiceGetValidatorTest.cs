using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using FluentAssertions;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceGetValidatorTest
{
    private readonly InvoiceGetValidator _validator = new();

    [Fact]
    public void Validate_GivenDefaultRequest_IsValid()
    {
        //Arrange
        var request = new InvoiceGetRequest();

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
        var request = new InvoiceGetRequest { Page = page, PageSize = PageSize.p50 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void Validate_GivenPageZero_ReturnsPageRangeMessage()
    {
        //Arrange
        var request = new InvoiceGetRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
    }

    [Fact]
    public void Validate_GivenAllFilters_IsValid()
    {
        //Arrange
        var request = new InvoiceGetRequest { UserId = Guid.NewGuid(), SellerId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    public static IEnumerable<TheoryDataRow<InvoiceGetRequest, string, string>> EmptyFilterTestData()
    {
        yield return new(new InvoiceGetRequest { UserId = Guid.Empty }, nameof(InvoiceGetRequest.UserId), "Please specify valid user Id") { Label = "empty user id is rejected" };
        yield return new(new InvoiceGetRequest { SellerId = Guid.Empty }, nameof(InvoiceGetRequest.SellerId), "Please specify valid seller Id") { Label = "empty seller id is rejected" };
        yield return new(new InvoiceGetRequest { CustomerId = Guid.Empty }, nameof(InvoiceGetRequest.CustomerId), "Please specify valid customer Id") { Label = "empty customer id is rejected" };
    }

    [Theory]
    [MemberData(nameof(EmptyFilterTestData))]
    public void Validate_GivenEmptyFilter_ReturnsItsMessage(InvoiceGetRequest request, string propertyName, string expectedMessage)
    {
        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(e => e.PropertyName == propertyName && e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_GivenUndefinedPageSize_IsInvalid()
    {
        //Arrange
        var request = new InvoiceGetRequest { PageSize = (PageSize)7 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.PageSize));
    }
}
