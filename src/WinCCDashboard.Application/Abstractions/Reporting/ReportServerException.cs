namespace WinCCDashboard.Application.Abstractions.Reporting;

public enum ReportServerErrorKind
{
    Unknown,
    ReportNotFound,
    AccessDenied,
    InvalidParameters,
    DataSourceError,
    Timeout,
    Unreachable,
    InvalidResponse
}

/// <summary>
/// Raised when SSRS cannot render a report or return its metadata.
/// <see cref="Exception.Message"/> is safe to show to end users: it never
/// contains server URLs, credentials or the raw SSRS error body.
/// </summary>
public sealed class ReportServerException : Exception
{
    public ReportServerException(
        ReportServerErrorKind kind,
        string userMessage,
        string? ssrsErrorCode = null,
        Exception? innerException = null)
        : base(userMessage, innerException)
    {
        Kind = kind;
        SsrsErrorCode = ssrsErrorCode;
    }

    public ReportServerErrorKind Kind { get; }

    /// <summary>SSRS error code such as rsItemNotFound, when one was returned.</summary>
    public string? SsrsErrorCode { get; }
}
