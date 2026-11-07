using BillerContracts.Enums;
using BillerContracts.Requests;
using FluentAssertions;
using Validators;

namespace xUnitTests.Validators;

public class PagedRequestValidatorTest
{
    private record TestPagedRequest : PagedRequest;

    private class TestPagedRequestValidator : PagedRequestValidator<TestPagedRequest>;

    private readonly TestPagedRequestValidator _validator = new();

    [Fact]
    public void Validate_GivenDefaultRequest_IsValid()
    {
        //Arrange
        var request = new TestPagedRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, Label = "first page is accepted")]
    [InlineData(0, Label = "page 0 is accepted, the repositories answer it with page 1")]
    [InlineData(-1, Label = "negative page is accepted, the repositories answer it with page 1")]
    [InlineData(int.MinValue, Label = "lowest int page is accepted, the repositories answer it with page 1")]
    [InlineData(42949673, Label = "page whose offset would overflow an int is accepted, the repositories answer it with the last page")]
    [InlineData(int.MaxValue, Label = "highest int page is accepted, the repositories answer it with the last page")]
    public void Validate_GivenAnyPage_IsValid(int page)
    {
        //Arrange
        var request = new TestPagedRequest { Page = page, PageSize = PageSize.p50 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenUndefinedPageSize_ReturnsPageSizeMessage()
    {
        //Arrange
        var request = new TestPagedRequest { PageSize = (PageSize)7 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.PageSize))
            .Which.ErrorMessage.Should().Be("Please specify valid page size");
    }
}
