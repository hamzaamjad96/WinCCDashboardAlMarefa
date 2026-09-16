using Microsoft.Data.SqlClient;
using System.Data;
using WinCCCustomDashboardMarefa.Models;

namespace WinCCCustomDashboardMarefa.Services;

public class DashboardService
{
    private readonly IConfiguration _configuration;

    public DashboardService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("ReportingDatabase")
        ?? throw new InvalidOperationException(
            "ReportingDatabase connection string not found.");

    public async Task<DashboardViewModel> GetDashboardDataAsync(
    int trendDays = 7,
    CancellationToken cancellationToken = default)
    {
        var model = new DashboardViewModel();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // =========================================
        // BLOCK 1 - Main dashboard SP
        // (reader is scoped and disposed before block 2 runs)
        // =========================================
        await using (var command = new SqlCommand(
            "dbo.usp_GetDashboardData", connection)
        {
            CommandType = CommandType.StoredProcedure
        })
        {
            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            // RS1 - Summary
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Summary.TotalAlarms = reader.GetInt64(0);
                model.Summary.TodayAlarms = reader.GetInt64(1);
                model.Summary.HighPriorityAlarms = reader.GetInt64(2);
            }

            // RS2 - Recent alarms
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    model.RecentAlarms.Add(new AlarmEvent
                    {
                        Id = reader.GetInt64(0),
                        AlarmName = reader.GetString(1),
                        RaiseTime = DateTime.SpecifyKind(
                            reader.GetDateTime(2), DateTimeKind.Utc),
                        Priority = reader.IsDBNull(3)
                            ? null
                            : reader.GetInt32(3),
                        SourceServer = reader.GetString(4),
                        SyncedAt = DateTime.SpecifyKind(
                            reader.GetDateTime(5), DateTimeKind.Utc)
                    });
                }
            }

            // RS3 - Sync status
            if (await reader.NextResultAsync(cancellationToken) &&
                await reader.ReadAsync(cancellationToken))
            {
                model.SyncStatus = new SyncStatus
                {
                    SyncName = reader.GetString(0),

                    LastSuccessfulFetchTime = reader.IsDBNull(1)
                        ? null
                        : DateTime.SpecifyKind(
                            reader.GetDateTime(1), DateTimeKind.Utc),

                    LastFetchStartedAt = reader.IsDBNull(2)
                        ? null
                        : DateTime.SpecifyKind(
                            reader.GetDateTime(2), DateTimeKind.Utc),

                    LastFetchCompletedAt = reader.IsDBNull(3)
                        ? null
                        : DateTime.SpecifyKind(
                            reader.GetDateTime(3), DateTimeKind.Utc),

                    LastSourceServer = reader.IsDBNull(4)
                        ? null
                        : reader.GetString(4),

                    LastStatus = reader.GetString(5),

                    LastError = reader.IsDBNull(6)
                        ? null
                        : reader.GetString(6),

                    UpdatedAt = DateTime.SpecifyKind(
                        reader.GetDateTime(7), DateTimeKind.Utc)
                };

                model.Summary.ActiveServer =
                    model.SyncStatus.LastSourceServer ?? "N/A";
            }
        }
        // <-- reader yahan dispose ho gaya, ab dusra reader khul sakta hai


        // =========================================
        // BLOCK 2 - Charts SP
        // =========================================
        await using (var chartCommand = new SqlCommand(
            "dbo.usp_GetDashboardCharts", connection)
        {
            CommandType = CommandType.StoredProcedure
        })
        {
            chartCommand.Parameters.AddWithValue("@TrendDays", trendDays);

            await using var chartReader =
                await chartCommand.ExecuteReaderAsync(cancellationToken);

            // RS1 - Trend
            while (await chartReader.ReadAsync(cancellationToken))
            {
                model.AlarmTrend.Add(new AlarmTrendPoint
                {
                    Date = chartReader.GetDateTime(0),
                    Count = chartReader.GetInt64(1)
                });
            }

            // RS2 - Priority
            if (await chartReader.NextResultAsync(cancellationToken))
            {
                while (await chartReader.ReadAsync(cancellationToken))
                {
                    model.AlarmsByPriority.Add(new AlarmCategoryPoint
                    {
                        Name = chartReader.GetString(0),
                        Count = chartReader.GetInt64(1)
                    });
                }
            }

            // RS3 - Server
            if (await chartReader.NextResultAsync(cancellationToken))
            {
                while (await chartReader.ReadAsync(cancellationToken))
                {
                    model.AlarmsByServer.Add(new AlarmCategoryPoint
                    {
                        Name = chartReader.GetString(0),
                        Count = chartReader.GetInt64(1)
                    });
                }
            }
        }

        return model;
    }
}