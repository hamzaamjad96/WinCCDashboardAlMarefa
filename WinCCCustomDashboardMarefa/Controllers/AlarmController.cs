using Microsoft.AspNetCore.Mvc;
using WinCCCustomDashboardMarefa.Services;

namespace WinCCCustomDashboardMarefa.Controllers;

public class AlarmController : Controller
{
    private readonly AlarmService _alarmService;

    public AlarmController(AlarmService alarmService)
    {
        _alarmService = alarmService;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 10)
            pageSize = 50;

        if (pageSize > 500)
            pageSize = 500;

        var result =
            await _alarmService.GetAlarmsAsync(
                page,
                pageSize,
                search,
                priority,
                sourceServer,
                startDate,
                endDate,
                cancellationToken);

        return View(result);
    }
}