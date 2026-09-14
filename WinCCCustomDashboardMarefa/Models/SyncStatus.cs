namespace WinCCCustomDashboardMarefa.Models;

public class SyncStatus
{
    public string SyncName { get; set; } = string.Empty;
    public DateTime? LastSuccessfulFetchTime { get; set; }
    public DateTime? LastFetchStartedAt { get; set; }
    public DateTime? LastFetchCompletedAt { get; set; }
    public string? LastSourceServer { get; set; }
    public string LastStatus { get; set; } = string.Empty;
    public string? LastError { get; set; }
    public DateTime UpdatedAt { get; set; }
}