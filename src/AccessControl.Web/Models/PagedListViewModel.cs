namespace AccessControl.Web.Models;

public class PagedListViewModel<T>
{
    private static readonly int[] AllowedPageSizes = [10, 25, 50, 100];

    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = AllowedPageSizes[1];
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public string Sort { get; init; } = string.Empty;
    public bool Desc { get; init; }
    public IReadOnlyList<int> PageSizeOptions => AllowedPageSizes;
    public int StartItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int EndItem => TotalCount == 0 ? 0 : Math.Min(TotalCount, Page * PageSize);

    public static PagedListViewModel<T> Create(IEnumerable<T> source, int page, int pageSize, string? sort, bool desc)
    {
        var normalizedPageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : AllowedPageSizes[1];
        var items = source.ToList();
        var totalCount = items.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
        var normalizedPage = Math.Clamp(page, 1, totalPages);

        return new PagedListViewModel<T>
        {
            Items = items
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            Sort = sort ?? string.Empty,
            Desc = desc
        };
    }
}
