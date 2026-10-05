using Microsoft.AspNetCore.Mvc;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Application.Abstractions.Reporting;

namespace WinCCCustomDashboardMarefa.Controllers;

public class ReportsController : Controller
{
    private readonly IReportDefinitionRepository _reports;
    private readonly ISsrsReportClient _ssrs;
    private readonly IConfiguration _config;

    public ReportsController(
        IReportDefinitionRepository reports,
        ISsrsReportClient ssrs,
        IConfiguration config)
    {
        _reports = reports;
        _ssrs = ssrs;
        _config = config;
    }

    // -----------------------------------------------------------
    // Report list page (grouped by category, from DB)
    // -----------------------------------------------------------
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var reports = await _reports.GetAllAsync(cancellationToken);

        ViewBag.SsrsUrl = _config["SsrsSettings:PublicUrl"];
        ViewBag.Folder = _config["SsrsSettings:ReportsFolder"];

        return View(reports);
    }

    // -----------------------------------------------------------
    // Report viewer (iframe embeds SSRS)
    // -----------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Show(
        string key,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetByKeyAsync(key, cancellationToken);
        if (report == null)
            return NotFound($"Report '{key}' not found.");

        ViewBag.SsrsUrl = _config["SsrsSettings:PublicUrl"];
        ViewBag.Folder = _config["SsrsSettings:ReportsFolder"];

        return View(report);
    }

    // -----------------------------------------------------------
    // Generic PDF download (all reports)
    // -----------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Export(
    string key,
    string? search = null,
    int? priority = null,
    string? sourceServer = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetByKeyAsync(key, cancellationToken);
        if (report == null)
            return NotFound($"Report '{key}' not found.");

        // Base parameters
        var parameters = new Dictionary<string, string?>
        {
            ["Search"] = search ?? string.Empty,
            ["Priority"] = priority?.ToString() ?? string.Empty,
            ["SourceServer"] = sourceServer ?? string.Empty,
            ["StartDate"] = (startDate ?? DateTime.UtcNow.AddDays(-30))
                .ToString("yyyy-MM-ddTHH:mm:ss"),
            ["EndDate"] = (endDate ?? DateTime.UtcNow)
                .ToString("yyyy-MM-ddTHH:mm:ss")
        };

        // Category-specific parameters
        if (report.ReportKey == "RedundancyEvents")
            parameters["Category"] = "Redundancy";
        else if (report.ReportKey == "SystemEvents")
            parameters["Category"] = "System";

        try
        {
            var bytes = await _ssrs.RenderReportAsync(
                report.RdlName, "PDF", parameters, cancellationToken);

            var fileName = $"{report.ReportKey}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return Content($"SSRS error: {ex.Message}", "text/plain");
        }
    }
}