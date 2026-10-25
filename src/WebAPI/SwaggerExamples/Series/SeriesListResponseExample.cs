using BillerContracts.Enums;
using BillerContracts.Responses.Series;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Series;

/// <summary>
/// example
/// </summary>
public class SeriesListResponseExample : IExamplesProvider<SeriesListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SeriesListResponse GetExamples()
    {
        SeriesListResponse seriesListResponse = new()
        {
            TotalCount = 2,
            Page = 1,
            PageSize = PageSize.p25
        };

        seriesListResponse.Series.Add(new SeriesResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
        });

        seriesListResponse.Series.Add(new SeriesResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "KL",
            NextNumber = 1
        });

        return seriesListResponse;
    }
}
