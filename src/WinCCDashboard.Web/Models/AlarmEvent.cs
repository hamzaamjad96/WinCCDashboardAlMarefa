namespace WinCCCustomDashboardMarefa.Models;

public class AlarmEvent
{
    public long Id { get; set; }
    public string AlarmName { get; set; } = string.Empty;
    public DateTime RaiseTime { get; set; }
    public int? Priority { get; set; }
    public string SourceServer { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
}