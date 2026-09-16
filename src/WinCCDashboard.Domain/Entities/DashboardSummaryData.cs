namespace WinCCDashboard.Domain.Entities;

public class DashboardSummaryData
{
    public long TotalAlarms { get; set; }
    public long TodayAlarms { get; set; }
    public long HighPriorityAlarms { get; set; }
    public string ActiveServer { get; set; } = "N/A";
}