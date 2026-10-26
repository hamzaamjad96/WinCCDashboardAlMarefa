namespace WinCCDashboard.Domain.Entities;

/// <summary>
/// "SsrsSettings" configuration section. Used only on the server:
/// none of these values are ever sent to the browser.
/// </summary>
public class SsrsSettings
{
    /// <summary>Report server web service URL, e.g. http://reportserver/ReportServer.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Folder that holds the deployed reports, e.g. /WinCCDashboard.</summary>
    public string ReportsFolder { get; set; } = string.Empty;

    public int TimeoutMinutes { get; set; } = 5;

    /// <summary>How long report parameter metadata and filter lookups are cached.</summary>
    public int MetadataCacheMinutes { get; set; } = 10;

    /// <summary>
    /// Optional service account used to call SSRS. Leave empty to use the
    /// identity of the web application process (IIS app pool / service account).
    /// Keep the password out of appsettings.json: use user-secrets or the
    /// environment variable SsrsSettings__Password.
    /// </summary>
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Domain { get; set; }
}
