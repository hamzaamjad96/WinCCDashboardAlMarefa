using Microsoft.Data.SqlClient;
using System.Data;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Persistence.Repositories;

public class ReportDefinitionRepository : IReportDefinitionRepository
{
    private readonly ISqlConnectionFactory _factory;

    public ReportDefinitionRepository(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<ReportDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<ReportDefinition>();

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_GetReportDefinitions", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            result.Add(Map(reader));

        return result;
    }

    public async Task<ReportDefinition?> GetByKeyAsync(
        string reportKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportKey))
            return null;

        await using var connection = await _factory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT ReportKey, Title, Description, Category, RdlName, SortOrder
            FROM dbo.ReportDefinitions
            WHERE ReportKey = @ReportKey AND IsActive = 1;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ReportKey", SqlDbType.NVarChar, 100).Value = reportKey;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return Map(reader);
    }

    private static ReportDefinition Map(SqlDataReader reader)
    {
        return new ReportDefinition
        {
            ReportKey = reader.GetString(reader.GetOrdinal("ReportKey")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description")),
            Category = reader.GetString(reader.GetOrdinal("Category")),
            RdlName = reader.GetString(reader.GetOrdinal("RdlName")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
        };
    }
}