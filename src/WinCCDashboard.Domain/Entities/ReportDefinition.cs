namespace WinCCDashboard.Domain.Entities;

public class ReportDefinition
{
    public string ReportKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string RdlName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}