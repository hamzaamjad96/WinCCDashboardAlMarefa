namespace WinCCDashboard.Application.Reports;

public class ReportDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RdlName { get; set; } = string.Empty;
}

public static class ReportCatalog
{
    public static readonly List<ReportDefinition> All = new()
    {
        new()
        {
            Key = "AlarmHistory",
            Title = "Alarm History",
            Description = "Complete alarm event history with date range filter.",
            Category = "Alarms",
            RdlName = "AlarmHistory"
        },
        new()
        {
            Key = "DailySummary",
            Title = "Daily Summary",
            Description = "Daily alarm counts grouped by priority and server.",
            Category = "Summaries",
            RdlName = "DailySummary"
        },
        new()
        {
            Key = "HourlyAnalysis",
            Title = "Hourly Analysis",
            Description = "Alarm activity distributed by hour of day.",
            Category = "Summaries",
            RdlName = "HourlyAnalysis"
        },
        new()
        {
            Key = "TopAlarms",
            Title = "Top Alarms",
            Description = "Most frequently occurring alarms.",
            Category = "Analysis",
            RdlName = "TopAlarms"
        },
        new()
        {
            Key = "PriorityAnalysis",
            Title = "Priority Analysis",
            Description = "Alarm distribution by priority with percentages.",
            Category = "Analysis",
            RdlName = "PriorityAnalysis"
        },
        new()
        {
            Key = "ServerAnalysis",
            Title = "Server Analysis",
            Description = "Compare alarm activity across servers.",
            Category = "Analysis",
            RdlName = "ServerAnalysis"
        },
        new()
        {
            Key = "RedundancyEvents",
            Title = "Redundancy Events",
            Description = "Redundancy and failover related alarms.",
            Category = "Events",
            RdlName = "RedundancyEvents"
        },
        new()
        {
            Key = "SystemEvents",
            Title = "System / Connection Events",
            Description = "System, connection and communication events.",
            Category = "Events",
            RdlName = "SystemEvents"
        }
    };

    public static ReportDefinition? GetByKey(string key) =>
        All.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
}