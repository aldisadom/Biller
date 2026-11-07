using BillerContracts.Requests;
using BillerContracts.Responses;

namespace Application.MappingProfiles;

public static class PagedResponseExtensions
{
    // page is the one the repository returned, which differs from query.Page when that is out of range
    public static T WithPaging<T>(this T response, PagedRequest query, int totalCount, int page) where T : PagedResponse
    {
        response.TotalCount = totalCount;
        response.Page = page;
        response.PageSize = query.PageSize;

        return response;
    }
}
