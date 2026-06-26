namespace Kentos.SharedKernel.Pagination;

/// <summary>Standard paging/sorting/search request for every list endpoint.</summary>
public class PagedRequest
{
    public const int MaxPageSize = 200;
    public const int DefaultPageSize = 20;

    private int _pageSize = DefaultPageSize;
    private int _page = 1;

    /// <summary>1-based page number.</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>Items per page (1..200, default 20).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? DefaultPageSize : value;
    }

    /// <summary>Sort expression, e.g. "name" or "createdAt desc".</summary>
    public string? Sort { get; set; }

    /// <summary>Free-text search term.</summary>
    public string? Search { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
