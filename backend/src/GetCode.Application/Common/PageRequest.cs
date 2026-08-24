namespace GetCode.Application.Common;

/// <summary>
/// Normalized paging input for read models. Values are clamped so callers
/// cannot request unbounded result sets.
/// </summary>
public sealed record PageRequest
{
    public const int MaxPageSize = 100;

    public int Page { get; }
    public int PageSize { get; }

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public static PageRequest Create(int page, int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }
        else if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        return new PageRequest(page, pageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>Deterministic page of results with total count for client-side navigation.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
