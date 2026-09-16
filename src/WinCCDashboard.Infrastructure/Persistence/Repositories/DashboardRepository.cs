using Microsoft.Data.SqlClient;
using System.Data;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Application.Dashboard.Dtos;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ISqlConnectionFactory _factory;

    public DashboardRepository(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<DashboardResult> GetDashboardDataAsync(
        int trendDays,
        CancellationToken cancellationToken = default)
    {
        var result = new DashboardResult();

        await using var connection = await _factory.CreateAsync(cancellationToken);

        // ---------- Main dashboard SP ----------
        await using (var command = new SqlCommand(
            "dbo.usp_GetDashboardData", connection)
        {
            CommandType = CommandType.StoredProcedure
        })
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // RS1 - Summary
            if (await reader.ReadAsync(cancellationToken))
            {
                result.Summary.TotalAlarms = reader.GetInt64(0);
                result.Summary.TodayAlarms = reader.GetInt64(1);
                result.Summary.HighPriorityAlarms = reader.GetInt64(2);
            }

            // RS2 - Recent alarms
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.RecentAlarms.Add(new AlarmEvent
                    {
                        Id = reader.GetInt64(0),
                        AlarmName = reader.GetString(1),
                        RaiseTime = DateTime.SpecifyKind(
                            reader.GetDateTime(2), DateTimeKind.Utc),
                        Priority = reader.IsDBNull(3) ? null : reader.GetInt32(3),
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
                result.SyncStatus = new SyncStatus
                {
                    SyncName = reader.GetString(0),
                    LastSuccessfulFetchTime = reader.IsDBNull(1) ? null
                        : DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                    LastFetchStartedAt = reader.IsDBNull(2) ? null
                        : DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                    LastFetchCompletedAt = reader.IsDBNull(3) ? null
                        : DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
                    LastSourceServer = reader.IsDBNull(4) ? null : reader.GetString(4),
                    LastStatus = reader.GetString(5),
                    LastError = reader.IsDBNull(6) ? null : reader.GetString(6),
                    UpdatedAt = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)
                };

                result.Summary.ActiveServer =
                    result.SyncStatus.LastSourceServer ?? "N/A";
            }
        }

        // ---------- Charts SP ----------
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
                result.AlarmTrend.Add(new AlarmTrendPoint
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
                    result.AlarmsByPriority.Add(new AlarmCategoryPoint
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
                    result.AlarmsByServer.Add(new AlarmCategoryPoint
                    {
                        Name = chartReader.GetString(0),
                        Count = chartReader.GetInt64(1)
                    });
                }
            }
        }

        return result;
    }
}