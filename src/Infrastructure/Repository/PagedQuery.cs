using Dapper;
using Domain.Helpers;
using System.Data;

namespace Infrastructure.Repository;

internal static class PagedQuery
{
    // dataSql selects the rows with COUNT(*) OVER() AS total_count as the last column and ends with
    // LIMIT @PageSize OFFSET @Offset. A page with rows takes one query. Only a page with no rows asks
    // for the count, then a page past the last one is read again as the last page.
    public static async Task<(IEnumerable<T> Items, int TotalCount, int Page)> QueryPage<T>(
        this IDbConnection dbConnection, string dataSql, string countSql, object filter, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        List<(T Row, int TotalCount)> rows = await QueryRows<T>(dbConnection, dataSql, filter, page, pageSize);

        if (rows.Count > 0)
            return (rows.Select(r => r.Row), rows[0].TotalCount, page);

        int totalCount = page == 1 ? 0 : await dbConnection.ExecuteScalarAsync<int>(countSql, filter);
        int lastPage = Paging.PageInBounds(page, pageSize, totalCount);

        if (totalCount == 0 || lastPage == page)
            return ([], totalCount, lastPage);

        rows = await QueryRows<T>(dbConnection, dataSql, filter, lastPage, pageSize);

        return (rows.Select(r => r.Row), rows.Count > 0 ? rows[0].TotalCount : totalCount, lastPage);
    }

    private static async Task<List<(T Row, int TotalCount)>> QueryRows<T>(
        IDbConnection dbConnection, string dataSql, object filter, int page, int pageSize)
    {
        var parameters = new DynamicParameters(filter);
        parameters.Add("PageSize", pageSize);
        // A long: (page - 1) * pageSize overflows an int for pages near int.MaxValue
        parameters.Add("Offset", (long)(page - 1) * pageSize);

        IEnumerable<(T Row, int TotalCount)> rows = await dbConnection.QueryAsync<T, long, (T, int)>(
            dataSql, (row, totalCount) => (row, (int)totalCount), parameters, splitOn: "total_count");

        return rows.ToList();
    }
}
