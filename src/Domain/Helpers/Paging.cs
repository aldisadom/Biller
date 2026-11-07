namespace Domain.Helpers;

public static class Paging
{
    // A page past the last one becomes the last page; with no rows the last page is 1
    public static int PageInBounds(int page, int pageSize, int totalCount)
    {
        int lastPage = Math.Max(1, totalCount / pageSize + (totalCount % pageSize > 0 ? 1 : 0));

        return Math.Clamp(page, 1, lastPage);
    }
}
