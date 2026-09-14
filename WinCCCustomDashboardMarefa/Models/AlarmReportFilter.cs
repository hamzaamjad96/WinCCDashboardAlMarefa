namespace WinCCCustomDashboardMarefa.Models;

public class AlarmReportFilter
{
    public string? Search { get; set; }
    public int? Priority { get; set; }
    public string? SourceServer { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Category { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TopN { get; set; } = 20;
}