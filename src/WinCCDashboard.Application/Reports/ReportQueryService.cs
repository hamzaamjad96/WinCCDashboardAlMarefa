using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Domain.Common;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Reports;

public class ReportQueryService
{
    private readonly IReportRepository _repository;

    public ReportQueryService(IReportRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AlarmEvent>> GetAlarmHistoryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        NormalizeFilter(filter);
        return _repository.GetAlarmHistoryAsync(filter, cancellationToken);
    }

    public Task<List<AlarmEvent>> GetAlarmHistoryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetAlarmHistoryForExportAsync(filter, cancellationToken);
    }

    public Task<PagedResult<DailyAlarmSummary>> GetDailyAlarmSummaryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        NormalizeFilter(filter);
        return _repository.GetDailyAlarmSummaryAsync(filter, cancellationToken);
    }

    public Task<List<DailyAlarmSummary>> GetDailyAlarmSummaryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetDailyAlarmSummaryForExportAsync(filter, cancellationToken);
    }

    private static void NormalizeFilter(AlarmReportFilter filter)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 10) filter.PageSize = 50;
        if (filter.PageSize > 500) filter.PageSize = 500;
    }
}