using WinCCDashboard.Application.Dashboard.Dtos;

namespace WinCCDashboard.Application.Abstractions.Persistence;

public interface IDashboardRepository
{
    Task<DashboardResult> GetDashboardDataAsync(
        int trendDays,
        CancellationToken cancellationToken = default);
}