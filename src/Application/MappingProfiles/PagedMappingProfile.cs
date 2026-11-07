using BillerContracts.Requests;
using BillerContracts.Responses;
using Domain.Helpers;

namespace Application.MappingProfiles;

public static class PagedResponseExtensions
{
    public static T WithPaging<T>(this T response, PagedRequest query, int totalCount) where T : PagedResponse
    {
        response.TotalCount = totalCount;
        // The repositories return the last page for a page past it, so the response names that page
        response.Page = Paging.PageInBounds(query.Page, (int)query.PageSize, totalCount);
        response.PageSize = query.PageSize;

        return response;
    }
}
