namespace WinCCDashboard.Domain.Entities;

public class DailyAlarmSummary
{
    public DateTime AlarmDate { get; set; }
    public long TotalAlarms { get; set; }
    public long HighPriorityAlarms { get; set; }
    public long Priority1Alarms { get; set; }
    public long Priority4Alarms { get; set; }
    public long Priority12Alarms { get; set; }
    public long Srv01Alarms { get; set; }
    public long Srv02Alarms { get; set; }
}