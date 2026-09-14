namespace WinCCCustomDashboardMarefa.Models;

public class DashboardViewModel
{
    public DashboardSummary Summary { get; set; } = new();

    public List<AlarmEvent> RecentAlarms { get; set; } = [];

    public SyncStatus? SyncStatus { get; set; }

    public List<AlarmTrendPoint> AlarmTrend { get; set; } = [];

    public List<AlarmCategoryPoint> AlarmsByPriority { get; set; } = [];

    public List<AlarmCategoryPoint> AlarmsByServer { get; set; } = [];
}

public class DashboardSummary
{
    public long TotalAlarms { get; set; }

    public long TodayAlarms { get; set; }

    public long HighPriorityAlarms { get; set; }

    public string ActiveServer { get; set; } = "N/A";
}


public class AlarmTrendPoint
{
    public DateTime Date { get; set; }

    public long Count { get; set; }
}

public class AlarmCategoryPoint
{
    public string Name { get; set; } = string.Empty;

    public long Count { get; set; }
}