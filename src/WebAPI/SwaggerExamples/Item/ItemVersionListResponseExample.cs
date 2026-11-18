using BillerContracts.Responses.Item;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Item;

/// <summary>
/// example
/// </summary>
public class ItemVersionListResponseExample : IExamplesProvider<ItemVersionListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public ItemVersionListResponse GetExamples()
    {
        return new ItemVersionListResponse()
        {
            ItemId = Guid.NewGuid(),
            Versions =
            [
                new ItemVersionResponse()
                {
                    Version = 1,
                    Name = "Iron throne",
                    Price = 500000m,
                    ChangedDate = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc)
                },
                new ItemVersionResponse()
                {
                    Version = 2,
                    Name = "Iron throne, melted swords",
                    Price = 999999999999999m,
                    ChangedDate = new DateTime(2026, 11, 10, 14, 30, 0, DateTimeKind.Utc)
                }
            ]
        };
    }
}
