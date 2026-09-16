using WinCCDashboard.Domain.Common;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Abstractions.Persistence;

public interface IAlarmRepository
{
    Task<PagedResult<AlarmEvent>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        int? priority,
        string? sourceServer,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);

    Task<List<AlarmEvent>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<SyncStatus?> GetSyncStatusAsync(
        string syncName,
        CancellationToken cancellationToken = default);
}