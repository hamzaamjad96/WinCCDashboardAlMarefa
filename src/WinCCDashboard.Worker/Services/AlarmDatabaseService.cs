using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WinCCReportingWorkerMarefa.Models;

namespace WinCCReportingWorkerMarefa.Services;

public class AlarmDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<AlarmDatabaseService> _logger;

    public AlarmDatabaseService(
        IConfiguration configuration,
        ILogger<AlarmDatabaseService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("ReportingDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'WinCCDatabase' was not found.");

        _logger = logger;
    }

    // ============================================
    // GET LAST SUCCESSFUL FETCH TIME
    // ============================================

    public async Task<DateTime?> GetLastSuccessfulFetchTimeAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT LastSuccessfulFetchTime
            FROM dbo.AlarmSyncState
            WHERE SyncName = 'LoggedAlarms';
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        var result =
            await command.ExecuteScalarAsync(cancellationToken);

        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToDateTime(result);
    }

    // ============================================
    // INSERT ALARM
    // ============================================

    public async Task<bool> InsertAlarmAsync(
        LoggedAlarm alarm,
        string sourceServer,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(
                "dbo.usp_InsertAlarmEvent",
                connection);

        command.CommandType =
            System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@AlarmName",
            alarm.Name);

        command.Parameters.AddWithValue(
            "@RaiseTime",
            alarm.RaiseTime);

        command.Parameters.AddWithValue(
            "@Priority",
            (object?)alarm.Priority ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@SourceServer",
            sourceServer);

        var result =
            await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result) == 1;
    }

    // ============================================
    // UPDATE SYNC STATE - SUCCESS
    // ============================================

    public async Task UpdateSyncSuccessAsync(
        DateTime fetchStartedAt,
        DateTime fetchCompletedAt,
        DateTime successfulFetchTime,
        string sourceServer,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.AlarmSyncState
            SET
                LastSuccessfulFetchTime = @LastSuccessfulFetchTime,
                LastFetchStartedAt = @LastFetchStartedAt,
                LastFetchCompletedAt = @LastFetchCompletedAt,
                LastSourceServer = @LastSourceServer,
                LastStatus = 'Success',
                LastError = NULL,
                UpdatedAt = SYSUTCDATETIME()
            WHERE SyncName = 'LoggedAlarms';
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@LastSuccessfulFetchTime",
            successfulFetchTime);

        command.Parameters.AddWithValue(
            "@LastFetchStartedAt",
            fetchStartedAt);

        command.Parameters.AddWithValue(
            "@LastFetchCompletedAt",
            fetchCompletedAt);

        command.Parameters.AddWithValue(
            "@LastSourceServer",
            sourceServer);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // ============================================
    // UPDATE SYNC STATE - FAILED
    // ============================================

    public async Task UpdateSyncFailureAsync(
        DateTime fetchStartedAt,
        string error,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.AlarmSyncState
            SET
                LastFetchStartedAt = @LastFetchStartedAt,
                LastFetchCompletedAt = SYSUTCDATETIME(),
                LastStatus = 'Failed',
                LastError = @LastError,
                UpdatedAt = SYSUTCDATETIME()
            WHERE SyncName = 'LoggedAlarms';
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@LastFetchStartedAt",
            fetchStartedAt);

        command.Parameters.AddWithValue(
            "@LastError",
            error);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}