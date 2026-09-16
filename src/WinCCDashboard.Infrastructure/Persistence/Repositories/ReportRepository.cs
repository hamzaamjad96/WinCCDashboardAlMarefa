using Microsoft.Data.SqlClient;
using System.Data;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Domain.Common;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ISqlConnectionFactory _factory;

    public ReportRepository(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    // ---------------- Alarm History ----------------

    public async Task<PagedResult<AlarmEvent>> GetAlarmHistoryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new PagedResult<AlarmEvent>
        {
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_ReportAlarmHistory", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddAlarmHistoryParameters(command, filter);
        command.Parameters.AddWithValue("@Export", false);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            result.Items.Add(MapAlarm(reader));

        if (await reader.NextResultAsync(cancellationToken) &&
            await reader.ReadAsync(cancellationToken))
        {
            result.TotalCount = reader.GetInt64(
                reader.GetOrdinal("TotalCount"));
        }

        return result;
    }

    public async Task<List<AlarmEvent>> GetAlarmHistoryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var alarms = new List<AlarmEvent>();

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_ReportAlarmHistory", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@PageNumber", 1);
        command.Parameters.AddWithValue("@PageSize", 500);
        AddFilterParameters(command, filter);
        command.Parameters.AddWithValue("@Export", true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            alarms.Add(MapAlarm(reader));

        return alarms;
    }

    // ---------------- Daily Summary ----------------

    public async Task<PagedResult<DailyAlarmSummary>> GetDailyAlarmSummaryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new PagedResult<DailyAlarmSummary>
        {
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_ReportDailyAlarmSummary", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@PageNumber", filter.Page);
        command.Parameters.AddWithValue("@PageSize", filter.PageSize);
        AddPriorityServerDateParameters(command, filter);
        command.Parameters.AddWithValue("@Export", false);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            result.Items.Add(MapDailySummary(reader));

        if (await reader.NextResultAsync(cancellationToken) &&
            await reader.ReadAsync(cancellationToken))
        {
            result.TotalCount = reader.GetInt64(
                reader.GetOrdinal("TotalCount"));
        }

        return result;
    }

    public async Task<List<DailyAlarmSummary>> GetDailyAlarmSummaryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DailyAlarmSummary>();

        await using var connection = await _factory.CreateAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_ReportDailyAlarmSummary", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@PageNumber", 1);
        command.Parameters.AddWithValue("@PageSize", 500);
        AddPriorityServerDateParameters(command, filter);
        command.Parameters.AddWithValue("@Export", true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            result.Add(MapDailySummary(reader));

        return result;
    }

    // ---------------- Helpers ----------------

    private static void AddAlarmHistoryParameters(
        SqlCommand command, AlarmReportFilter filter)
    {
        command.Parameters.AddWithValue("@PageNumber", filter.Page);
        command.Parameters.AddWithValue("@PageSize", filter.PageSize);
        AddFilterParameters(command, filter);
    }

    private static void AddFilterParameters(
        SqlCommand command, AlarmReportFilter filter)
    {
        command.Parameters.Add(
            "@Search", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(filter.Search)
                ? DBNull.Value : filter.Search.Trim();

        command.Parameters.AddWithValue(
            "@Priority",
            (object?)filter.Priority ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@SourceServer",
            (object?)filter.SourceServer ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);
    }

    private static void AddPriorityServerDateParameters(
        SqlCommand command, AlarmReportFilter filter)
    {
        command.Parameters.AddWithValue(
            "@Priority",
            (object?)filter.Priority ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@SourceServer",
            (object?)filter.SourceServer ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);
    }

    private static AlarmEvent MapAlarm(SqlDataReader reader)
    {
        return new AlarmEvent
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            AlarmName = reader.GetString(reader.GetOrdinal("AlarmName")),
            RaiseTime = DateTime.SpecifyKind(
                reader.GetDateTime(reader.GetOrdinal("RaiseTime")),
                DateTimeKind.Utc),
            Priority = reader.IsDBNull(reader.GetOrdinal("Priority"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("Priority")),
            SourceServer = reader.GetString(reader.GetOrdinal("SourceServer")),
            SyncedAt = DateTime.SpecifyKind(
                reader.GetDateTime(reader.GetOrdinal("SyncedAt")),
                DateTimeKind.Utc)
        };
    }

    private static DailyAlarmSummary MapDailySummary(SqlDataReader reader)
    {
        return new DailyAlarmSummary
        {
            AlarmDate = reader.GetDateTime(reader.GetOrdinal("AlarmDate")),
            TotalAlarms = reader.GetInt64(reader.GetOrdinal("TotalAlarms")),
            HighPriorityAlarms = reader.GetInt64(reader.GetOrdinal("HighPriorityAlarms")),
            Priority1Alarms = reader.GetInt64(reader.GetOrdinal("Priority1Alarms")),
            Priority4Alarms = reader.GetInt64(reader.GetOrdinal("Priority4Alarms")),
            Priority12Alarms = reader.GetInt64(reader.GetOrdinal("Priority12Alarms")),
            Srv01Alarms = reader.GetInt64(reader.GetOrdinal("Srv01Alarms")),
            Srv02Alarms = reader.GetInt64(reader.GetOrdinal("Srv02Alarms"))
        };
    }
}