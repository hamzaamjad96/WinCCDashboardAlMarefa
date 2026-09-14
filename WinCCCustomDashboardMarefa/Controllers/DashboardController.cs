using Microsoft.AspNetCore.Mvc;
using WinCCCustomDashboardMarefa.Services;

namespace WinCCCustomDashboardMarefa.Controllers;

public class DashboardController : Controller
{
    private readonly DashboardService _dashboardService;

    public DashboardController(
        DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model =
            await _dashboardService.GetDashboardDataAsync(
                cancellationToken);

        return View(model);
    }
}