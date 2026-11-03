using BillerContracts.Requests.Invoice;
using FluentAssertions;
using FluentValidation.Results;
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
            .Which.Should().Match<ValidationFailure>(e => e.PropertyName == propertyName && e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_GivenPageZero_AppliesPagingRules()
    {
        //Arrange
        var request = new InvoiceGetRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
    }
}
