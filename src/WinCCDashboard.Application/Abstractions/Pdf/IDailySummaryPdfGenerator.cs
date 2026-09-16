using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Abstractions.Pdf;

public interface IDailySummaryPdfGenerator
{
    byte[] Generate(List<DailyAlarmSummary> data);
}