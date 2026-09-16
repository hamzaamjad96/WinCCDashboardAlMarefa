using Microsoft.AspNetCore.Mvc;
using WinCCDashboard.Application.Alarms;

namespace WinCCCustomDashboardMarefa.Controllers;

public class AlarmController : Controller
{
    private readonly AlarmQueryService _alarmService;

    public AlarmController(AlarmQueryService alarmService)
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
        var result = await _alarmService.GetAlarmsAsync(
            page, pageSize, search, priority,
            sourceServer, startDate, endDate, cancellationToken);

        return View(result);
    }
}