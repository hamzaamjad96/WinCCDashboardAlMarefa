using WinCCDashboard.Domain.Common;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task<PagedResult<AlarmEvent>> GetAlarmHistoryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<List<AlarmEvent>> GetAlarmHistoryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DailyAlarmSummary>> GetDailyAlarmSummaryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<List<DailyAlarmSummary>> GetDailyAlarmSummaryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Distinct priorities and source servers for report filter dropdowns.</summary>
    Task<ReportFilterLookups> GetFilterLookupsAsync(
        CancellationToken cancellationToken = default);
}