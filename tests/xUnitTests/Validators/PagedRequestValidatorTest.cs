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
    [InlineData(1, true, Label = "first page is accepted")]
    [InlineData(42949672, true, Label = "highest page whose offset fits in an int is accepted")]
    [InlineData(0, false, Label = "page 0 is rejected")]
    [InlineData(-1, false, Label = "negative page is rejected")]
    [InlineData(42949673, false, Label = "page whose offset overflows an int is rejected")]
    public void Validate_GivenPage_ReturnsExpected(int page, bool expected)
    {
        //Arrange
        var request = new TestPagedRequest { Page = page, PageSize = PageSize.p50 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void Validate_GivenPageZero_ReturnsPageRangeMessage()
    {
        //Arrange
        var request = new TestPagedRequest { Page = 0 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Page))
            .Which.ErrorMessage.Should().Be("Please specify page from 1 to 42949672");
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

    [Fact]
    public void MaxPage_IsHighestPageWhoseOffsetFitsInAnIntAtTheLargestPageSize()
    {
        //Act
        int maxPage = PagedRequestValidator.MaxPage;

        //Assert
        maxPage.Should().Be(int.MaxValue / (int)PageSize.p50);
    }
}
