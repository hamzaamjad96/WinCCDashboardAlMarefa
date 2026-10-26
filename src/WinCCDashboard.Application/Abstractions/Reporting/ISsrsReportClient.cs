namespace WinCCDashboard.Application.Abstractions.Reporting;

/// <summary>
/// Server-side gateway to SQL Server Reporting Services.
/// The browser never talks to SSRS directly: all rendering and metadata
/// requests go through this client using the web server's credentials.
/// </summary>
public interface ISsrsReportClient
{
    /// <summary>
    /// Renders a report through SSRS URL access and returns the raw output.
    /// A parameter value of <c>null</c> is sent as an explicit NULL
    /// (<c>Name:isnull=true</c>); an empty string is sent as a blank value.
    /// Repeat a key to pass several values to a multi-value parameter.
    /// </summary>
    Task<byte[]> RenderReportAsync(
        string reportName,
        string format = "PDF",
        IEnumerable<KeyValuePair<string, string?>>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the parameter definitions of a deployed report from SSRS
    /// (ReportService2010.asmx GetItemParameters).
    /// </summary>
    Task<IReadOnlyList<ReportServerParameter>> GetReportParametersAsync(
        string reportName,
        CancellationToken cancellationToken = default);
}
