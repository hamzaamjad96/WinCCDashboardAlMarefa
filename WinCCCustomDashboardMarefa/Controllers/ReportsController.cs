using Microsoft.AspNetCore.Mvc;
using WinCCCustomDashboardMarefa.Models;
using WinCCCustomDashboardMarefa.Pdf;
using WinCCCustomDashboardMarefa.Services;

namespace WinCCCustomDashboardMarefa.Controllers;

public class ReportsController : Controller
{
    private readonly ReportsService _reportsService;
    private readonly AlarmHistoryPdfService _alarmHistoryPdfService;
    private readonly DailyAlarmSummaryPdfService _dailyAlarmSummaryPdfService;

    public ReportsController(
        ReportsService reportsService,
        AlarmHistoryPdfService alarmHistoryPdfService,
        DailyAlarmSummaryPdfService dailyAlarmSummaryPdfService)
    {
        _reportsService = reportsService;
        _alarmHistoryPdfService = alarmHistoryPdfService;
        _dailyAlarmSummaryPdfService = dailyAlarmSummaryPdfService;
    }

    // ==================== Alarm History ====================

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
        var filter = BuildFilter(page, pageSize, search, priority,
            sourceServer, startDate, endDate);

        var alarmHistory = await _reportsService.GetAlarmHistoryAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "AlarmHistory";

        return View(alarmHistory);
    }

    // ==================== Daily Summary ====================

    public async Task<IActionResult> DailySummary(
        int page = 1,
        int pageSize = 50,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(page, pageSize, null, priority,
            sourceServer, startDate, endDate);

        var result = await _reportsService.GetDailyAlarmSummaryAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "DailySummary";

        return View(result);
    }

    // ==================== Hourly Analysis ====================

    public async Task<IActionResult> HourlyAnalysis(
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(1, 50, null, priority,
            sourceServer, startDate, endDate);

        var data = await _reportsService.GetHourlyAnalysisAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "HourlyAnalysis";

        return View(data);
    }

    // ==================== Top Alarms ====================

    public async Task<IActionResult> TopAlarms(
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int topN = 20,
        CancellationToken cancellationToken = default)
    {
        if (topN < 1) topN = 20;
        if (topN > 100) topN = 100;

        var filter = BuildFilter(1, 50, null, priority,
            sourceServer, startDate, endDate);
        filter.TopN = topN;

        var data = await _reportsService.GetTopAlarmsAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "TopAlarms";

        return View(data);
    }

    // ==================== Priority Analysis ====================

    public async Task<IActionResult> PriorityAnalysis(
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(1, 50, null, null,
            sourceServer, startDate, endDate);

        var data = await _reportsService.GetPriorityAnalysisAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "PriorityAnalysis";

        return View(data);
    }

    // ==================== Server Analysis ====================

    public async Task<IActionResult> ServerAnalysis(
        int? priority = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(1, 50, null, priority,
            null, startDate, endDate);

        var data = await _reportsService.GetServerAnalysisAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "ServerAnalysis";

        return View(data);
    }

    // ==================== Redundancy Events ====================

    public async Task<IActionResult> RedundancyEvents(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(page, pageSize, search, priority,
            sourceServer, startDate, endDate);
        filter.Category = "Redundancy";

        var result = await _reportsService.GetCategoryEventsAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "RedundancyEvents";

        return View(result);
    }

    // ==================== System Events ====================

    public async Task<IActionResult> SystemEvents(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(page, pageSize, search, priority,
            sourceServer, startDate, endDate);
        filter.Category = "System";

        var result = await _reportsService.GetCategoryEventsAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "SystemEvents";

        return View(result);
    }

    // ==================== PDF Exports ====================

    [HttpGet]
    public async Task<IActionResult> ExportAlarmHistory(
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(1, 500, search, priority,
            sourceServer, startDate, endDate);

        var alarms = await _reportsService.GetAlarmHistoryForExportAsync(
            filter, cancellationToken);

        var pdf = _alarmHistoryPdfService.Generate(alarms);
        var fileName = $"AlarmHistory_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportDailySummary(
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(1, 500, null, priority,
            sourceServer, startDate, endDate);

        var data = await _reportsService.GetDailyAlarmSummaryForExportAsync(
            filter, cancellationToken);

        var pdf = _dailyAlarmSummaryPdfService.Generate(data);
        var fileName = $"DailyAlarmSummary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCategoryEvents(
        string category,
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(1, 500, search, priority,
            sourceServer, startDate, endDate);
        filter.Category = category;

        var alarms = await _reportsService.GetCategoryEventsForExportAsync(
            filter, cancellationToken);

        var pdf = _alarmHistoryPdfService.Generate(alarms);
        var fileName = $"{category}Events_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    // ==================== Helper ====================

    private static AlarmReportFilter BuildFilter(
        int page, int pageSize, string? search, int? priority,
        string? sourceServer, DateTime? startDate, DateTime? endDate)
    {
        if (page < 1) page = 1;
        if (pageSize < 10) pageSize = 50;
        if (pageSize > 500) pageSize = 500;

        return new AlarmReportFilter
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Priority = priority,
            SourceServer = sourceServer,
            StartDate = startDate,
            EndDate = endDate
        };
    }
}