namespace WinCCReportingWorkerMarefa.Models;

public class LoggedAlarmGraphQLResponse
{
    public LoggedAlarmData? Data { get; set; }
}

public class LoggedAlarmData
{
    public List<LoggedAlarm> LoggedAlarms { get; set; } = [];
}