namespace WinCCReportingWorkerMarefa.Models;

public class LoggedAlarm
{
    public string Name { get; set; } = string.Empty;

    public DateTime RaiseTime { get; set; }

    public int? Priority { get; set; }
}