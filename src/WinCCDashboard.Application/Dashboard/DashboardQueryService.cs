using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Application.Dashboard.Dtos;

namespace WinCCDashboard.Application.Dashboard;

public class DashboardQueryService
{
    private readonly IDashboardRepository _repository;

    public DashboardQueryService(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public Task<DashboardResult> GetDashboardAsync(
        int trendDays,
        CancellationToken cancellationToken = default)
    {
        if (trendDays < 1) trendDays = 7;
        if (trendDays > 90) trendDays = 90;

        return _repository.GetDashboardDataAsync(trendDays, cancellationToken);
    }
}