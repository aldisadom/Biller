using BillerContracts.Requests.Series;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Series;

/// <summary>
/// example
/// </summary>
public class SeriesAddRequestExample : IExamplesProvider<SeriesAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SeriesAddRequest GetExamples()
    {
        return new SeriesAddRequest()
        {
            SellerId = Guid.NewGuid(),
            Name = "WF"
        };
    }
}
