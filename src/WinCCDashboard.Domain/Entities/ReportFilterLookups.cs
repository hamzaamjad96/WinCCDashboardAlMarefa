namespace WinCCDashboard.Domain.Entities;

/// <summary>
/// Distinct values used to populate report filter dropdowns.
/// </summary>
public class ReportFilterLookups
{
    public List<int> Priorities { get; set; } = new();
    public List<string> SourceServers { get; set; } = new();
}
