using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WinCCDashboard.Application.Abstractions.Reporting;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Reporting;

/// <summary>
/// Talks to SSRS on behalf of the web application (server-side only).
/// Rendering uses URL access (/ReportServer?/Folder/Report&amp;rs:Format=...),
/// parameter metadata uses the ReportService2010 SOAP endpoint.
/// </summary>
public class SsrsReportClient : ISsrsReportClient
{
    private const string SoapNamespace =
        "http://schemas.microsoft.com/sqlserver/reporting/2010/03/01/ReportServer";

    private static readonly XNamespace Rs = SoapNamespace;
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private static readonly Regex SsrsErrorCodeRegex =
        new(@"\b(rs[A-Z][A-Za-z]+)\b", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly SsrsSettings _settings;
    private readonly ILogger<SsrsReportClient> _logger;

    public SsrsReportClient(
        HttpClient httpClient,
        IOptions<SsrsSettings> settings,
        ILogger<SsrsReportClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromMinutes(
            _settings.TimeoutMinutes > 0 ? _settings.TimeoutMinutes : 5);
    }

    // ------------------------------------------------------------------
    // Rendering
    // ------------------------------------------------------------------

    public async Task<byte[]> RenderReportAsync(
        string reportName,
        string format = "PDF",
        IEnumerable<KeyValuePair<string, string?>>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportName))
            throw new ArgumentException("Report name is required.", nameof(reportName));

        var url = BuildRenderUrl(reportName, format, parameters);
        _logger.LogInformation("SSRS render {Report} as {Format}: {Url}", reportName, format, url);

        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, url), reportName, cancellationToken);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw CreateError(response.StatusCode, Decode(bytes), reportName);

        // SSRS sometimes answers 200 with an HTML error page (or a parameter
        // prompt page) instead of the requested document. Never forward that
        // to the browser as a "PDF".
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase)
            && !format.StartsWith("HTML", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateError(HttpStatusCode.OK, Decode(bytes), reportName);
        }

        if (format.Equals("PDF", StringComparison.OrdinalIgnoreCase) && !LooksLikePdf(bytes))
        {
            throw new ReportServerException(
                ReportServerErrorKind.InvalidResponse,
                "The report server returned an unexpected response instead of a PDF document.");
        }

        return bytes;
    }

    private string BuildRenderUrl(
        string reportName,
        string format,
        IEnumerable<KeyValuePair<string, string?>>? parameters)
    {
        var sb = new StringBuilder(BaseUrl());
        sb.Append('?').Append(EscapePath(ItemPath(reportName)));
        sb.Append("&rs:Command=Render");
        sb.Append("&rs:Format=").Append(Uri.EscapeDataString(format));

        // Parse DateTime parameter values culture-independently.
        // Without this SSRS uses the server culture (e.g. ar-SA uses the
        // Hijri calendar) and ISO dates can be misread.
        sb.Append("&rs:ParameterLanguage=en-US");

        if (parameters == null)
            return sb.ToString();

        foreach (var (name, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains(':'))
                continue; // never let callers inject rs:/rc: commands

            var escapedName = Uri.EscapeDataString(name);

            if (value == null)
                sb.Append('&').Append(escapedName).Append(":isnull=true");
            else
                sb.Append('&').Append(escapedName).Append('=').Append(Uri.EscapeDataString(value));
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // Parameter metadata (SOAP: ReportService2010.GetItemParameters)
    // ------------------------------------------------------------------

    public async Task<IReadOnlyList<ReportServerParameter>> GetReportParametersAsync(
        string reportName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportName))
            throw new ArgumentException("Report name is required.", nameof(reportName));

        var url = BaseUrl() + "/ReportService2010.asmx";

        var envelope =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "<soap:Body>" +
            $"<GetItemParameters xmlns=\"{SoapNamespace}\">" +
            $"<ItemPath>{SecurityElement.Escape(ItemPath(reportName))}</ItemPath>" +
            "<ForRendering>true</ForRendering>" +
            "</GetItemParameters>" +
            "</soap:Body>" +
            "</soap:Envelope>";

        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"\"{SoapNamespace}/GetItemParameters\"");
            return request;
        }, reportName, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw CreateError(response.StatusCode, body, reportName);

        try
        {
            return ParseItemParameters(body);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException)
        {
            _logger.LogError(ex, "Could not parse SSRS GetItemParameters response for {Report}", reportName);
            throw new ReportServerException(
                ReportServerErrorKind.InvalidResponse,
                "The report server returned report parameters in an unexpected format.",
                innerException: ex);
        }
    }

    internal static IReadOnlyList<ReportServerParameter> ParseItemParameters(string soapXml)
    {
        var doc = XDocument.Parse(soapXml);
        var result = new List<ReportServerParameter>();

        foreach (var p in doc.Descendants(Rs + "ItemParameter"))
        {
            result.Add(new ReportServerParameter
            {
                Name = (string?)p.Element(Rs + "Name") ?? string.Empty,
                DataType = (string?)p.Element(Rs + "ParameterTypeName") ?? "String",
                Nullable = Bool(p, "Nullable"),
                AllowBlank = Bool(p, "AllowBlank"),
                MultiValue = Bool(p, "MultiValue"),
                PromptUser = p.Element(Rs + "PromptUser") == null || Bool(p, "PromptUser"),
                Prompt = (string?)p.Element(Rs + "Prompt"),
                DefaultValuesQueryBased = Bool(p, "DefaultValuesQueryBased"),
                DefaultValues = p.Element(Rs + "DefaultValues")?
                    .Elements(Rs + "Value")
                    .Select(v => IsNil(v) ? null : v.Value)
                    .ToList() ?? new List<string?>(),
                ValidValuesQueryBased = Bool(p, "ValidValuesQueryBased"),
                ValidValues = p.Element(Rs + "ValidValues")?
                    .Elements(Rs + "ValidValue")
                    .Select(v => new ReportParameterOption(
                        (string?)v.Element(Rs + "Value") ?? string.Empty,
                        (string?)v.Element(Rs + "Label") ?? (string?)v.Element(Rs + "Value") ?? string.Empty))
                    .ToList() ?? new List<ReportParameterOption>()
            });
        }

        return result;

        static bool Bool(XElement parent, string name) =>
            bool.TryParse((string?)parent.Element(Rs + name), out var b) && b;

        static bool IsNil(XElement e) =>
            string.Equals((string?)e.Attribute(Xsi + "nil"), "true", StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        string reportName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new ReportServerException(
                ReportServerErrorKind.Unreachable,
                "Reporting is not configured. Set SsrsSettings:BaseUrl on the web server.");
        }

        using var request = requestFactory();

        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "SSRS request for {Report} timed out", reportName);
            throw new ReportServerException(
                ReportServerErrorKind.Timeout,
                "The report server took too long to respond. Try a shorter date range or more specific filters.",
                innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "SSRS request for {Report} failed", reportName);
            throw new ReportServerException(
                ReportServerErrorKind.Unreachable,
                "The report server could not be reached. Please try again later or contact your administrator.",
                innerException: ex);
        }
    }

    private ReportServerException CreateError(HttpStatusCode status, string body, string reportName)
    {
        var code = ExtractErrorCode(body);

        _logger.LogError(
            "SSRS error for {Report}: HTTP {Status}, code {Code}. Body: {Body}",
            reportName, (int)status, code ?? "(none)", Truncate(body, 4000));

        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            || code is "rsAccessDenied" or "rsAccessDeniedToSecureData")
        {
            return new ReportServerException(
                ReportServerErrorKind.AccessDenied,
                "The web application is not allowed to run this report. Ask your administrator to grant the application's account access on the report server.",
                code);
        }

        return code switch
        {
            "rsItemNotFound" or "rsInvalidItemPath" or "rsWrongItemType" =>
                new ReportServerException(
                    ReportServerErrorKind.ReportNotFound,
                    $"The report '{reportName}' is not deployed on the report server. Check the report folder and the RdlName configured for this report.",
                    code),

            "rsUnknownReportParameter" or "rsReadOnlyReportParameter" or "rsParameterTypeMismatch"
                or "rsInvalidReportParameter" or "rsReportParameterValueNotSet"
                or "rsParametersNotSpecified" or "rsMissingParameter" or "rsReportParameterTypeMismatch" =>
                new ReportServerException(
                    ReportServerErrorKind.InvalidParameters,
                    "The report rejected one or more filter values. Check the filters and try again.",
                    code),

            "rsErrorOpeningConnection" or "rsErrorExecutingCommand" or "rsDataSourceNotFound"
                or "rsInvalidDataSourceReference" or "rsInvalidDataSourceCredentialSetting"
                or "rsProcessingAborted" =>
                new ReportServerException(
                    ReportServerErrorKind.DataSourceError,
                    "The report server could not read the report data. Check the report's shared data source on the report server.",
                    code),

            _ => new ReportServerException(
                ReportServerErrorKind.Unknown,
                "The report server could not generate this report. Please try again or contact your administrator.",
                code)
        };
    }

    internal static string? ExtractErrorCode(string body)
    {
        if (string.IsNullOrEmpty(body))
            return null;

        // SOAP faults carry <ErrorCode>rsXxx</ErrorCode>; URL access HTML shows "(rsXxx)".
        // Prefer the most specific (innermost / last listed) processing error
        // only when the first code is the generic wrapper.
        var matches = SsrsErrorCodeRegex.Matches(body)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        if (matches.Count == 0)
            return null;

        var specific = matches.FirstOrDefault(c => c is not "rsProcessingAborted" and not "rsInternalError");
        return specific ?? matches[0];
    }

    private string BaseUrl() => _settings.BaseUrl.TrimEnd('/');

    private string ItemPath(string reportName)
    {
        var folder = (_settings.ReportsFolder ?? string.Empty).Trim().Trim('/');
        var name = reportName.Trim().Trim('/');
        return folder.Length == 0 ? "/" + name : "/" + folder + "/" + name;
    }

    private static string EscapePath(string path) =>
        string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length > 4 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F';

    private static string Decode(byte[] bytes)
    {
        try { return Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 64 * 1024)); }
        catch { return string.Empty; }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
