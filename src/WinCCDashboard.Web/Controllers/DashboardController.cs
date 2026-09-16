using Microsoft.AspNetCore.Mvc;
using WinCCDashboard.Application.Dashboard;
using WinCCDashboard.Web.ViewModels.Dashboard;

namespace WinCCCustomDashboardMarefa.Controllers;

public class DashboardController : Controller
{
    private readonly DashboardQueryService _dashboardService;

    public DashboardController(DashboardQueryService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(
        int trendDays = 7,
        CancellationToken cancellationToken = default)
    {
        var data = await _dashboardService.GetDashboardAsync(
            trendDays, cancellationToken);

        var model = new DashboardViewModel
        {
            Summary = data.Summary,
            RecentAlarms = data.RecentAlarms,
            SyncStatus = data.SyncStatus,
            AlarmTrend = data.AlarmTrend,
            AlarmsByPriority = data.AlarmsByPriority,
            AlarmsByServer = data.AlarmsByServer
        };

        ViewBag.TrendDays = trendDays;
        return View(model);
    }
}