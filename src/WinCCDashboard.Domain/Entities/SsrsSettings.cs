namespace WinCCDashboard.Domain.Entities;

public class SsrsSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ReportsFolder { get; set; } = string.Empty;
    public int TimeoutMinutes { get; set; } = 5;
}