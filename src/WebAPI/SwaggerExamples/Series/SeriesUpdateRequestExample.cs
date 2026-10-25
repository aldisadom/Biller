using BillerContracts.Requests.Series;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Series;

/// <summary>
/// example
/// </summary>
public class SeriesUpdateRequestExample : IExamplesProvider<SeriesUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SeriesUpdateRequest GetExamples()
    {
        return new SeriesUpdateRequest()
        {
            Id = Guid.NewGuid(),
            Name = "KL",
            NextNumber = 7
        };
    }
}
