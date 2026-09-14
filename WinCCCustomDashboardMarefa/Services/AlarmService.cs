using Microsoft.Data.SqlClient;
using System.Data;
using WinCCCustomDashboardMarefa.Models;

namespace WinCCCustomDashboardMarefa.Services;

public class AlarmService
{
    private readonly IConfiguration _configuration;

    public AlarmService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("ReportingDatabase")
        ?? throw new InvalidOperationException(
            "ReportingDatabase connection string not found.");

    public async Task<PagedResult<AlarmEvent>> GetAlarmsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? search = null,
        int? priority = null,
        string? sourceServer = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = new PagedResult<AlarmEvent>
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(
                "dbo.usp_GetAlarmEventsPaged",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@PageNumber",
            SqlDbType.Int).Value = pageNumber;

        command.Parameters.Add(
            "@PageSize",
            SqlDbType.Int).Value = pageSize;

        command.Parameters.Add(
            "@Search",
            SqlDbType.NVarChar,
            500).Value =
            string.IsNullOrWhiteSpace(search)
                ? DBNull.Value
                : search;

        command.Parameters.Add(
            "@Priority",
            SqlDbType.Int).Value =
            priority.HasValue
                ? priority.Value
                : DBNull.Value;

        command.Parameters.Add(
            "@SourceServer",
            SqlDbType.NVarChar,
            100).Value =
            string.IsNullOrWhiteSpace(sourceServer)
                ? DBNull.Value
                : sourceServer;

        command.Parameters.Add(
            "@StartDate",
            SqlDbType.DateTime2).Value =
            startDate.HasValue
                ? startDate.Value
                : DBNull.Value;

        command.Parameters.Add(
            "@EndDate",
            SqlDbType.DateTime2).Value =
            endDate.HasValue
                ? endDate.Value
                : DBNull.Value;

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        // ==========================================
        // RESULT SET 1 - Alarms
        // ==========================================

        while (await reader.ReadAsync(
            cancellationToken))
        {
            result.Items.Add(new AlarmEvent
            {
                Id = reader.GetInt64(0),

                AlarmName = reader.GetString(1),

                RaiseTime = DateTime.SpecifyKind(
                    reader.GetDateTime(2),
                    DateTimeKind.Utc),

                Priority = reader.IsDBNull(3)
                    ? null
                    : reader.GetInt32(3),

                SourceServer = reader.GetString(4),

                SyncedAt = DateTime.SpecifyKind(
                    reader.GetDateTime(5),
                    DateTimeKind.Utc)
            });
        }

        // ==========================================
        // RESULT SET 2 - Total Count
        // ==========================================

        if (await reader.NextResultAsync(
            cancellationToken))
        {
            if (await reader.ReadAsync(
                cancellationToken))
            {
                result.TotalCount =
                    reader.GetInt64(0);
            }
        }

        return result;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(
    CancellationToken cancellationToken = default)
    {
        // Yahan dashboard-specific SP(s) call honge

        var model = new DashboardViewModel();

        // Summary
        // Recent alarms
        // Sync status
        // Trend
        // Priority
        // Server distribution

        return model;
    }

    public async Task<SyncStatus?> GetSyncStatusAsync(
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
        SELECT
            SyncName,
            LastSuccessfulFetchTime,
            LastFetchStartedAt,
            LastFetchCompletedAt,
            LastSourceServer,
            LastStatus,
            LastError,
            UpdatedAt
        FROM dbo.AlarmSyncState
        WHERE SyncName = 'LoggedAlarms';
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new SyncStatus
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
    }

    public async Task<List<AlarmEvent>> GetLatestAlarmsAsync(
    int limit = 10,
    CancellationToken cancellationToken = default)
    {
        var alarms = new List<AlarmEvent>();

        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(
                "dbo.usp_GetLatestAlarmEvents",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@Limit",
            SqlDbType.Int).Value = limit;

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            alarms.Add(new AlarmEvent
            {
                Id = reader.GetInt64(0),

                AlarmName = reader.GetString(1),

                RaiseTime = DateTime.SpecifyKind(
                    reader.GetDateTime(2),
                    DateTimeKind.Utc),

                Priority = reader.IsDBNull(3)
                    ? null
                    : reader.GetInt32(3),

                SourceServer = reader.GetString(4),

                SyncedAt = DateTime.SpecifyKind(
                    reader.GetDateTime(5),
                    DateTimeKind.Utc)
            });
        }

        return alarms;
    }
}