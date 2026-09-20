using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Application.Abstractions.Persistence;

public interface IReportDefinitionRepository
{
    Task<List<ReportDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ReportDefinition?> GetByKeyAsync(
        string reportKey,
        CancellationToken cancellationToken = default);
}