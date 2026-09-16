using Microsoft.AspNetCore.Mvc;
using WinCCDashboard.Application.Abstractions.Pdf;
using WinCCDashboard.Application.Reports;
using WinCCDashboard.Domain.Entities;

namespace WinCCCustomDashboardMarefa.Controllers;

public class ReportsController : Controller
{
    private readonly ReportQueryService _reportsService;
    private readonly IAlarmHistoryPdfGenerator _alarmHistoryPdfGenerator;
    private readonly IDailySummaryPdfGenerator _dailySummaryPdfGenerator;

    public ReportsController(
        ReportQueryService reportsService,
        IAlarmHistoryPdfGenerator alarmHistoryPdfGenerator,
        IDailySummaryPdfGenerator dailySummaryPdfGenerator)
    {
        _reportsService = reportsService;
        _alarmHistoryPdfGenerator = alarmHistoryPdfGenerator;
        _dailySummaryPdfGenerator = dailySummaryPdfGenerator;
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
        var filter = BuildFilter(page, pageSize, search, priority,
            sourceServer, startDate, endDate);

        var alarmHistory = await _reportsService.GetAlarmHistoryAsync(
            filter, cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.SelectedReport = "AlarmHistory";

        return View(alarmHistory);
    }

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

        var pdf = _alarmHistoryPdfGenerator.Generate(alarms);
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

        var pdf = _dailySummaryPdfGenerator.Generate(data);
        var fileName = $"DailyAlarmSummary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

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