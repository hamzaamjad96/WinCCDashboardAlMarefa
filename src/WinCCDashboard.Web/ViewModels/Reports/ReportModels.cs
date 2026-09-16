namespace WinCCDashboard.Web.ViewModels.Reports;

public class HourlyAnalysis
{
    public int Hour { get; set; }
    public long TotalAlarms { get; set; }
    public long HighPriorityAlarms { get; set; }
    public long Priority1Alarms { get; set; }
    public long Priority4Alarms { get; set; }
    public long Priority12Alarms { get; set; }
    public long Srv01Alarms { get; set; }
    public long Srv02Alarms { get; set; }
}

public class TopAlarm
{
    public string AlarmName { get; set; } = string.Empty;
    public long TotalCount { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public int? Priority { get; set; }
}

public class PriorityAnalysis
{
    public int? Priority { get; set; }
    public long TotalCount { get; set; }
    public decimal Percentage { get; set; }
    public long Srv01Count { get; set; }
    public long Srv02Count { get; set; }
}

public class ServerAnalysis
{
    public string SourceServer { get; set; } = string.Empty;
    public long TotalCount { get; set; }
    public long HighPriorityCount { get; set; }
    public long Priority1Count { get; set; }
    public long Priority4Count { get; set; }
    public long Priority12Count { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
}