using Microsoft.Data.SqlClient;
using System.Data;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Domain.Common;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Persistence.Repositories;

public class AlarmRepository : IAlarmRepository
{
    private readonly ISqlConnectionFactory _factory;

    public AlarmRepository(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<PagedResult<AlarmEvent>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        int? priority,
        string? sourceServer,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var result = new PagedResult<AlarmEvent>
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_GetAlarmEventsPaged", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        command.Parameters.Add("@Search", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(search) ? DBNull.Value : search;
        command.Parameters.Add("@Priority", SqlDbType.Int).Value =
            priority.HasValue ? priority.Value : DBNull.Value;
        command.Parameters.Add("@SourceServer", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(sourceServer) ? DBNull.Value : sourceServer;
        command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value =
            startDate.HasValue ? startDate.Value : DBNull.Value;
        command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value =
            endDate.HasValue ? endDate.Value : DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            result.Items.Add(MapAlarm(reader));

        if (await reader.NextResultAsync(cancellationToken) &&
            await reader.ReadAsync(cancellationToken))
        {
            result.TotalCount = reader.GetInt64(0);
        }

        return result;
    }

    public async Task<List<AlarmEvent>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var alarms = new List<AlarmEvent>();

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_GetLatestAlarmEvents", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@Limit", SqlDbType.Int).Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            alarms.Add(MapAlarm(reader));

        return alarms;
    }

    public async Task<SyncStatus?> GetSyncStatusAsync(
        string syncName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateAsync(cancellationToken);

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
            WHERE SyncName = @SyncName;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@SyncName", SqlDbType.NVarChar, 100).Value = syncName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new SyncStatus
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
    }

    private static AlarmEvent MapAlarm(SqlDataReader reader)
    {
        return new AlarmEvent
        {
            Id = reader.GetInt64(0),
            AlarmName = reader.GetString(1),
            RaiseTime = DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
            Priority = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            SourceServer = reader.GetString(4),
            SyncedAt = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)
        };
    }
}