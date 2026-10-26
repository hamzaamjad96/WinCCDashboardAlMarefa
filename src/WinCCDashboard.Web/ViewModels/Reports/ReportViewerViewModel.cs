using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Web.ViewModels.Reports;

public class ReportViewerViewModel
{
    public ReportDefinition Report { get; set; } = new();

    /// <summary>All active reports, for the report selector.</summary>
    public List<ReportDefinition> Reports { get; set; } = new();
}
