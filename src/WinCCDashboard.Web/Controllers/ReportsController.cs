using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Application.Abstractions.Reporting;
using WinCCDashboard.Application.Reports;
using WinCCDashboard.Web.ViewModels.Reports;

namespace WinCCCustomDashboardMarefa.Controllers;

/// <summary>
/// Reports module. SSRS is called only from the server: the browser asks
/// this controller for a rendered document (same origin), so no SSRS URL,
/// credential or Windows-auth prompt ever reaches the client.
/// </summary>
public class ReportsController : Controller
{
    private static readonly string[] ReservedQueryKeys = { "key", "format", "download", "_" };

    private static readonly Dictionary<string, (string ContentType, string Extension)> Formats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PDF"] = ("application/pdf", "pdf"),
            ["EXCELOPENXML"] = ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            ["WORDOPENXML"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
            ["CSV"] = ("text/csv", "csv")
        };

    private readonly IReportDefinitionRepository _reports;
    private readonly ISsrsReportClient _ssrs;
    private readonly ReportParameterService _parameters;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportDefinitionRepository reports,
        ISsrsReportClient ssrs,
        ReportParameterService parameters,
        ILogger<ReportsController> logger)
    {
        _reports = reports;
        _ssrs = ssrs;
        _parameters = parameters;
        _logger = logger;
    }

    // -----------------------------------------------------------
    // Report list page (grouped by category, from DB)
    // -----------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reports = await _reports.GetAllAsync(cancellationToken);
        return View(reports);
    }

    // -----------------------------------------------------------
    // Report viewer page: report selector + filters + viewer
    // -----------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Show(string key, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByKeyAsync(key, cancellationToken);
        if (report == null)
            return NotFound($"Report '{key}' not found.");

        var all = await _reports.GetAllAsync(cancellationToken);

        return View(new ReportViewerViewModel
        {
            Report = report,
            Reports = all
        });
    }

    // -----------------------------------------------------------
    // Filter definition for one report (JSON, built from SSRS metadata)
    // -----------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Parameters(string key, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByKeyAsync(key, cancellationToken);
        if (report == null)
            return Error(StatusCodes.Status404NotFound, "This report is not configured.");

        try
        {
            var definition = await _parameters.GetFilterDefinitionAsync(report, cancellationToken);
            Response.Headers.CacheControl = "no-store";
            return Json(definition);
        }
        catch (ReportServerException ex)
        {
            return ReportServerError(ex, report);
        }
    }

    // -----------------------------------------------------------
    // Render a report through SSRS and stream it back (same origin).
    // GET /Reports/Render?key=AlarmHistory&format=PDF&StartDate=2026-10-01&EndDate=2026-10-05
    // -----------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Render(
        string key,
        string format = "PDF",
        bool download = false,
        CancellationToken cancellationToken = default)
    {
        var values = Request.Query
            .Where(q => !ReservedQueryKeys.Contains(q.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(q => q.Key, q => q.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        return await RenderCoreAsync(key, format, download, values, rejectUnknown: true, cancellationToken);
    }

    // -----------------------------------------------------------
    // Legacy PDF download URL (kept for existing links/bookmarks)
    // -----------------------------------------------------------
    [HttpGet]
    public Task<IActionResult> Export(
        string key,
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string?[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Search"] = new[] { search },
            ["Priority"] = new[] { priority?.ToString(CultureInfo.InvariantCulture) },
            ["SourceServer"] = new[] { sourceServer },
            ["StartDate"] = new[] { startDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) },
            ["EndDate"] = new[] { endDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) }
        };

        return RenderCoreAsync(key, "PDF", download: true, values, rejectUnknown: false, cancellationToken);
    }

    private async Task<IActionResult> RenderCoreAsync(
        string key,
        string format,
        bool download,
        IReadOnlyDictionary<string, string?[]> values,
        bool rejectUnknown,
        CancellationToken cancellationToken)
    {
        if (!Formats.TryGetValue(format ?? string.Empty, out var output))
            return Error(StatusCodes.Status400BadRequest, "Unsupported export format.");

        var report = await _reports.GetByKeyAsync(key, cancellationToken);
        if (report == null)
            return Error(StatusCodes.Status404NotFound, "This report is not configured.");

        try
        {
            var build = await _parameters.BuildParametersAsync(report, values, rejectUnknown, cancellationToken);

            if (!build.IsValid)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "Please correct the highlighted filters.",
                    build.Errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
            }

            var bytes = await _ssrs.RenderReportAsync(
                report.RdlName, format!.ToUpperInvariant(), build.Parameters, cancellationToken);

            var fileName = $"{SafeFileName(report.ReportKey)}_{DateTime.Now:yyyyMMdd_HHmm}.{output.Extension}";

            Response.Headers.CacheControl = "no-store";

            if (download)
                return File(bytes, output.ContentType, fileName);

            var disposition = new ContentDispositionHeaderValue("inline");
            disposition.SetHttpFileName(fileName);
            Response.Headers.ContentDisposition = disposition.ToString();
            return File(bytes, output.ContentType);
        }
        catch (ReportServerException ex)
        {
            return ReportServerError(ex, report);
        }
    }

    private IActionResult ReportServerError(ReportServerException ex, WinCCDashboard.Domain.Entities.ReportDefinition report)
    {
        _logger.LogWarning(ex, "Report {ReportKey} ({RdlName}) failed: {Kind} {Code}",
            report.ReportKey, report.RdlName, ex.Kind, ex.SsrsErrorCode);

        var status = ex.Kind switch
        {
            ReportServerErrorKind.InvalidParameters => StatusCodes.Status400BadRequest,
            ReportServerErrorKind.ReportNotFound => StatusCodes.Status404NotFound,
            ReportServerErrorKind.Timeout => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status502BadGateway
        };

        return Error(status, ex.Message);
    }

    private static JsonResult Error(int status, string message, Dictionary<string, string[]>? errors = null) =>
        new(new { message, errors }) { StatusCode = status };

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "Report" : cleaned;
    }
}
