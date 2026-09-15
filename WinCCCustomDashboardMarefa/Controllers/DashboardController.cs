using Microsoft.AspNetCore.Mvc;
using WinCCCustomDashboardMarefa.Services;

namespace WinCCCustomDashboardMarefa.Controllers;

public class DashboardController : Controller
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(
        int trendDays = 7,
        CancellationToken cancellationToken = default)
    {
        if (trendDays < 1) trendDays = 7;
        if (trendDays > 90) trendDays = 90;

        var model = await _dashboardService.GetDashboardDataAsync(
            trendDays,
            cancellationToken);

        ViewBag.TrendDays = trendDays;

        return View(model);
    }
}