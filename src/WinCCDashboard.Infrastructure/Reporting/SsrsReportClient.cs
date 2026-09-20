using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using WinCCDashboard.Application.Abstractions.Reporting;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Reporting;

public class SsrsReportClient : ISsrsReportClient
{
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

        if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }

        _httpClient.Timeout = TimeSpan.FromMinutes(_settings.TimeoutMinutes);
    }

    public async Task<byte[]> RenderReportAsync(
        string reportName,
        string format = "PDF",
        Dictionary<string, string?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportName))
            throw new ArgumentException("Report name is required.", nameof(reportName));

        var relativeUrl = BuildReportUrl(reportName, format, parameters);
        _logger.LogInformation("SSRS request: {Url}", relativeUrl);

        var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SSRS returned {Status}. Body: {Body}",
                response.StatusCode, body);

            throw new InvalidOperationException(
                $"SSRS render failed ({response.StatusCode}).");
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private string BuildReportUrl(
        string reportName,
        string format,
        Dictionary<string, string?>? parameters)
    {
        var folder = _settings.ReportsFolder.TrimEnd('/');

        var sb = new StringBuilder();
        sb.Append($"?{folder}/{reportName}");
        sb.Append($"&rs:Format={Uri.EscapeDataString(format)}");
        sb.Append("&rs:Command=Render");

        if (parameters != null && parameters.Count > 0)
        {
            foreach (var (key, value) in parameters)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                sb.Append($"&{Uri.EscapeDataString(key)}=");
                sb.Append(Uri.EscapeDataString(value));
            }
        }

        return sb.ToString();
    }
}