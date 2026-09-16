using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public DashboardSummaryData Summary { get; set; } = new();
    public List<AlarmEvent> RecentAlarms { get; set; } = [];
    public SyncStatus? SyncStatus { get; set; }
    public List<AlarmTrendPoint> AlarmTrend { get; set; } = [];
    public List<AlarmCategoryPoint> AlarmsByPriority { get; set; } = [];
    public List<AlarmCategoryPoint> AlarmsByServer { get; set; } = [];
}