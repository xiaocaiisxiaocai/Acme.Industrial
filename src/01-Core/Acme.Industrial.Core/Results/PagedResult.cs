namespace Acme.Industrial.Core.Results;

/// <summary>
/// 分页结果。
/// </summary>
/// <typeparam name="T">数据类型。</typeparam>
public class PagedResult<T>
{
    /// <summary>数据项。</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>总数量。</summary>
    public int TotalCount { get; init; }

    /// <summary>页索引（从 0 开始）。</summary>
    public int PageIndex { get; init; }

    /// <summary>页大小。</summary>
    public int PageSize { get; init; }

    /// <summary>总页数。</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>是否有上一页。</summary>
    public bool HasPrevious => PageIndex > 0;

    /// <summary>是否有下一页。</summary>
    public bool HasNext => PageIndex < TotalPages - 1;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
