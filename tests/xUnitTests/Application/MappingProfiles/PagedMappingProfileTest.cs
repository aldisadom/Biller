using Application.MappingProfiles;
using BillerContracts.Enums;
using BillerContracts.Requests.Series;
using BillerContracts.Responses.Series;
using AwesomeAssertions;

namespace xUnitTests.Application.MappingProfiles;

public class PagedMappingProfileTest
{
    [Fact]
    public void WithPaging_GivenQueryAndTotalCount_SetsPagingAndKeepsList()
    {
        //Arrange
        var series = new List<SeriesResponse> { new() { Id = Guid.NewGuid(), Name = "WF" } };
        var response = new SeriesListResponse { Series = series };
        var query = new SeriesGetRequest { SellerId = Guid.NewGuid(), Page = 3, PageSize = PageSize.p50 };

        //Act
        var result = response.WithPaging(query, 120, 3);

        //Assert
        result.Should().BeSameAs(response);
        result.Should().BeEquivalentTo(new SeriesListResponse
        {
            Series = series,
            TotalCount = 120,
            Page = 3,
            PageSize = PageSize.p50
        });
    }

    [Fact]
    public void WithPaging_GivenReturnedPageOtherThanRequested_SetsReturnedPage()
    {
        //Arrange
        var response = new SeriesListResponse();
        var query = new SeriesGetRequest { Page = 9, PageSize = PageSize.p25 };

        //Act
        var result = response.WithPaging(query, 60, 3);

        //Assert
        result.Page.Should().Be(3);
    }
}
