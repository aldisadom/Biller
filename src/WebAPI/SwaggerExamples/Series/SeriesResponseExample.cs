using BillerContracts.Responses.Series;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Series;

/// <summary>
/// example
/// </summary>
public class SeriesResponseExample : IExamplesProvider<SeriesResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SeriesResponse GetExamples()
    {
        return new SeriesResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
        };
    }
}
