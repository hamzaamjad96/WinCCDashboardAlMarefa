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
        CancellationToken cancellationToken = default)
    {
        var model = new DashboardViewModel();

        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(
                "dbo.usp_GetDashboardData",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        // =========================================
        // RESULT SET 1 - SUMMARY
        // =========================================

        if (await reader.ReadAsync(cancellationToken))
        {
            model.Summary.TotalAlarms =
                reader.GetInt64(0);

            model.Summary.TodayAlarms =
                reader.GetInt64(1);

            model.Summary.HighPriorityAlarms =
                reader.GetInt64(2);
        }

        // =========================================
        // RESULT SET 2 - LATEST ALARMS
        // =========================================

        if (await reader.NextResultAsync(
            cancellationToken))
        {
            while (await reader.ReadAsync(
                cancellationToken))
            {
                model.RecentAlarms.Add(
                    new AlarmEvent
                    {
                        Id = reader.GetInt64(0),

                        AlarmName = reader.GetString(1),

                        RaiseTime =
                            DateTime.SpecifyKind(
                                reader.GetDateTime(2),
                                DateTimeKind.Utc),

                        Priority =
                            reader.IsDBNull(3)
                                ? null
                                : reader.GetInt32(3),

                        SourceServer =
                            reader.GetString(4),

                        SyncedAt =
                            DateTime.SpecifyKind(
                                reader.GetDateTime(5),
                                DateTimeKind.Utc)
                    });
            }
        }

        // =========================================
        // RESULT SET 3 - SYNC STATUS
        // =========================================

        if (await reader.NextResultAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SyncStatus = new SyncStatus
                {
                    SyncName = reader.GetString(0),

                    LastSuccessfulFetchTime =
                        reader.IsDBNull(1)
                            ? null
                            : DateTime.SpecifyKind(
                                reader.GetDateTime(1),
                                DateTimeKind.Utc),

                    LastFetchStartedAt =
                        reader.IsDBNull(2)
                            ? null
                            : DateTime.SpecifyKind(
                                reader.GetDateTime(2),
                                DateTimeKind.Utc),

                    LastFetchCompletedAt =
                        reader.IsDBNull(3)
                            ? null
                            : DateTime.SpecifyKind(
                                reader.GetDateTime(3),
                                DateTimeKind.Utc),

                    LastSourceServer =
                        reader.IsDBNull(4)
                            ? null
                            : reader.GetString(4),

                    LastStatus =
                        reader.GetString(5),

                    LastError =
                        reader.IsDBNull(6)
                            ? null
                            : reader.GetString(6),

                    UpdatedAt =
                        DateTime.SpecifyKind(
                            reader.GetDateTime(7),
                            DateTimeKind.Utc)
                };

                // Dashboard card
                model.Summary.ActiveServer =
                    model.SyncStatus.LastSourceServer ?? "N/A";
            }
        }

        return model;
    }
}