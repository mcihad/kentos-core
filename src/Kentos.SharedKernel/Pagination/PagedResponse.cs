namespace Kentos.SharedKernel.Pagination;

/// <summary>Paginated list response envelope.</summary>
public sealed class PagedResponse<T>
{
    public PagedResponse(IReadOnlyList<T> items, long totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public long TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages { get; }

    public static PagedResponse<T> Empty(PagedRequest request) =>
        new([], 0, request.Page, request.PageSize);
}
