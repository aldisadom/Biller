using Domain.Helpers;
using FluentAssertions;

namespace xUnitTests.Domain.Helpers;

public class PagingTest
{
    [Theory]
    [InlineData(1, 25, 0, 1, Label = "no rows, first page stays 1")]
    [InlineData(3, 25, 0, 1, Label = "no rows, page past the end becomes 1")]
    [InlineData(0, 25, 60, 1, Label = "page 0 becomes 1")]
    [InlineData(-1, 25, 60, 1, Label = "negative page becomes 1")]
    [InlineData(int.MinValue, 25, 60, 1, Label = "lowest int page becomes 1")]
    [InlineData(-1, 25, 0, 1, Label = "no rows, negative page becomes 1")]
    [InlineData(2, 25, 60, 2, Label = "page inside the rows is kept")]
    [InlineData(3, 25, 60, 3, Label = "last partly filled page is kept")]
    [InlineData(4, 25, 60, 3, Label = "page right after the last becomes the last")]
    [InlineData(2, 25, 50, 2, Label = "last full page is kept")]
    [InlineData(3, 25, 50, 2, Label = "page after a last full page becomes the last")]
    [InlineData(int.MaxValue, 10, 5, 1, Label = "very large page becomes the last")]
    [InlineData(5, 10, int.MaxValue, 5, Label = "count near int max does not overflow")]
    [InlineData(int.MaxValue, 10, int.MaxValue, 214748365, Label = "largest count rounds its last page up")]
    public void PageInBounds_GivenPageAndCount_ReturnsExpected(int page, int pageSize, int totalCount, int expected)
    {
        //Act
        int result = Paging.PageInBounds(page, pageSize, totalCount);

        //Assert
        result.Should().Be(expected);
    }
}
