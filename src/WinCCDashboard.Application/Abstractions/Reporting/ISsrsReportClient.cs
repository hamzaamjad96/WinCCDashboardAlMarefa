namespace WinCCDashboard.Application.Abstractions.Reporting;

public interface ISsrsReportClient
{
    Task<byte[]> RenderReportAsync(
        string reportName,
        string format = "PDF",
        Dictionary<string, string?>? parameters = null,
        CancellationToken cancellationToken = default);
}