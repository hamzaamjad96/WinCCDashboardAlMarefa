using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Domain.Common;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Alarms;

public class AlarmQueryService
{
    private readonly IAlarmRepository _repository;

    public AlarmQueryService(IAlarmRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AlarmEvent>> GetAlarmsAsync(
        int page,
        int pageSize,
        string? search,
        int? priority,
        string? sourceServer,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 10) pageSize = 50;
        if (pageSize > 500) pageSize = 500;

        return _repository.GetPagedAsync(
            page, pageSize, search, priority,
            sourceServer, startDate, endDate, cancellationToken);
    }
}