using BillerContracts.Requests;
using BillerContracts.Responses;

namespace Application.MappingProfiles;

public static class PagedResponseExtensions
{
    public static T WithPaging<T>(this T response, PagedRequest query, int totalCount) where T : PagedResponse
    {
        response.TotalCount = totalCount;
        response.Page = query.Page;
        response.PageSize = query.PageSize;

        return response;
    }
}
