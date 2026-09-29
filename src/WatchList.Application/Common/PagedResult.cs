namespace WatchList.Application.Common;

/// <param name="PageIndex">The page actually returned, already clamped to <see cref="TotalPages"/>.</param>
/// <param name="FilteredCount">Items that matched the search, across every page.</param>
/// <param name="TotalCount">Items in the list before the search.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageIndex, int PageSize, int FilteredCount, int TotalCount)
{
    /// <summary>Never zero: an empty result still has one (empty) page.</summary>
    public int TotalPages => FilteredCount == 0 ? 1 : ((FilteredCount - 1) / PageSize) + 1;

    public bool HasPreviousPage => PageIndex > 0;

    public bool HasNextPage => PageIndex < TotalPages - 1;
}

internal static class PagedResultExtensions
{
    public static PagedResult<T> ToPagedResult<T>(this IReadOnlyList<T> filtered, PageRequest request, int totalCount)
    {
        var lastPage = filtered.Count == 0 ? 0 : (filtered.Count - 1) / request.PageSize;
        var pageIndex = Math.Min(request.PageIndex, lastPage);
        var items = filtered.Skip(pageIndex * request.PageSize).Take(request.PageSize).ToList();

        return new PagedResult<T>(items, pageIndex, request.PageSize, filtered.Count, totalCount);
    }
}
