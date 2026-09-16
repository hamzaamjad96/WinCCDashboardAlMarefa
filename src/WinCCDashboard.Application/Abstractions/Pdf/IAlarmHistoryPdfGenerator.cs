using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Abstractions.Pdf;

public interface IAlarmHistoryPdfGenerator
{
    byte[] Generate(List<AlarmEvent> alarms);
}