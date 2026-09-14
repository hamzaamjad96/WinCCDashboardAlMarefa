namespace WinCCCustomDashboardMarefa.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public long TotalCount { get; set; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);
}