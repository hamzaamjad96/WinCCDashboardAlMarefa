namespace WinCCReportingWorkerMarefa.Models;

public class SyncSettings
{
    public int IntervalSeconds { get; set; }

    public int OverlapSeconds { get; set; }

    public int InitialFetchMinutes { get; set; }

    public int MaxAlarmsPerRequest { get; set; }
}