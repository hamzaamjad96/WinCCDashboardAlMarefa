using Microsoft.Data.SqlClient;
using System.Data;
using WinCCCustomDashboardMarefa.Models;

namespace WinCCCustomDashboardMarefa.Services;

public class ReportsService
{
    private readonly string _connectionString;

    public ReportsService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("ReportingDatabase")
            ?? throw new InvalidOperationException(
                "ReportingDatabase connection string is not configured.");
    }

    public async Task<PagedResult<AlarmEvent>> GetAlarmHistoryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new PagedResult<AlarmEvent>
        {
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "dbo.usp_ReportAlarmHistory",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.AddWithValue(
            "@PageNumber", filter.Page);

        command.Parameters.AddWithValue(
            "@PageSize", filter.PageSize);

        command.Parameters.Add(
    "@Search",
    SqlDbType.NVarChar,
    500).Value =
        string.IsNullOrWhiteSpace(filter.Search)
            ? DBNull.Value
            : filter.Search.Trim();

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

        command.Parameters.AddWithValue(
            "@Export", false);

        await connection.OpenAsync(cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        // Result set 1: alarms
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Items.Add(MapAlarm(reader));
        }

        // Result set 2: total count
        if (await reader.NextResultAsync(cancellationToken) &&
            await reader.ReadAsync(cancellationToken))
        {
            result.TotalCount =
                reader.GetInt64(
                    reader.GetOrdinal("TotalCount"));
        }

        return result;
    }

    public async Task<List<AlarmEvent>> GetAlarmHistoryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var alarms = new List<AlarmEvent>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "dbo.usp_ReportAlarmHistory",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.AddWithValue("@PageNumber", 1);
        command.Parameters.AddWithValue("@PageSize", 500);

        command.Parameters.Add(
    "@Search",
    SqlDbType.NVarChar,
    500).Value =
        string.IsNullOrWhiteSpace(filter.Search)
            ? DBNull.Value
            : filter.Search.Trim();

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

        command.Parameters.AddWithValue("@Export", true);

        await connection.OpenAsync(cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            alarms.Add(MapAlarm(reader));
        }

        return alarms;
    }

    public async Task<PagedResult<DailyAlarmSummary>>
    GetDailyAlarmSummaryAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result =
            new PagedResult<DailyAlarmSummary>
            {
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "dbo.usp_ReportDailyAlarmSummary",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.AddWithValue(
            "@PageNumber",
            filter.Page);

        command.Parameters.AddWithValue(
            "@PageSize",
            filter.PageSize);

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

        command.Parameters.AddWithValue(
            "@Export",
            false);

        await connection.OpenAsync(
            cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        // Result set 1
        while (await reader.ReadAsync(
            cancellationToken))
        {
            result.Items.Add(
                new DailyAlarmSummary
                {
                    AlarmDate =
                        reader.GetDateTime(
                            reader.GetOrdinal(
                                "AlarmDate")),

                    TotalAlarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "TotalAlarms")),

                    HighPriorityAlarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "HighPriorityAlarms")),

                    Priority1Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Priority1Alarms")),

                    Priority4Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Priority4Alarms")),

                    Priority12Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Priority12Alarms")),

                    Srv01Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Srv01Alarms")),

                    Srv02Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Srv02Alarms"))
                });
        }

        // Result set 2
        if (await reader.NextResultAsync(
                cancellationToken) &&
            await reader.ReadAsync(
                cancellationToken))
        {
            result.TotalCount =
                reader.GetInt64(
                    reader.GetOrdinal(
                        "TotalCount"));
        }

        return result;
    }

    public async Task<List<DailyAlarmSummary>>
    GetDailyAlarmSummaryForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result =
            new List<DailyAlarmSummary>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "dbo.usp_ReportDailyAlarmSummary",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.AddWithValue(
            "@PageNumber",
            1);

        command.Parameters.AddWithValue(
            "@PageSize",
            500);

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

        command.Parameters.AddWithValue(
            "@Export",
            true);

        await connection.OpenAsync(
            cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(
            cancellationToken))
        {
            result.Add(
                new DailyAlarmSummary
                {
                    AlarmDate =
                        reader.GetDateTime(
                            reader.GetOrdinal(
                                "AlarmDate")),

                    TotalAlarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "TotalAlarms")),

                    HighPriorityAlarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "HighPriorityAlarms")),

                    Priority1Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Priority1Alarms")),

                    Priority4Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Priority4Alarms")),

                    Priority12Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Priority12Alarms")),

                    Srv01Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Srv01Alarms")),

                    Srv02Alarms =
                        reader.GetInt64(
                            reader.GetOrdinal(
                                "Srv02Alarms"))
                });
        }

        return result;
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
    // ==================== NEW METHODS ====================

    public async Task<List<HourlyAnalysis>> GetHourlyAnalysisAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new List<HourlyAnalysis>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "dbo.usp_ReportHourlyAnalysis", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Priority",
            (object?)filter.Priority ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceServer",
            string.IsNullOrWhiteSpace(filter.SourceServer)
                ? DBNull.Value : filter.SourceServer);
        command.Parameters.AddWithValue("@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new HourlyAnalysis
            {
                Hour = reader.GetInt32(reader.GetOrdinal("Hour")),
                TotalAlarms = reader.GetInt64(reader.GetOrdinal("TotalAlarms")),
                HighPriorityAlarms = reader.GetInt64(reader.GetOrdinal("HighPriorityAlarms")),
                Priority1Alarms = reader.GetInt64(reader.GetOrdinal("Priority1Alarms")),
                Priority4Alarms = reader.GetInt64(reader.GetOrdinal("Priority4Alarms")),
                Priority12Alarms = reader.GetInt64(reader.GetOrdinal("Priority12Alarms")),
                Srv01Alarms = reader.GetInt64(reader.GetOrdinal("Srv01Alarms")),
                Srv02Alarms = reader.GetInt64(reader.GetOrdinal("Srv02Alarms"))
            });
        }

        return result;
    }

    public async Task<List<TopAlarm>> GetTopAlarmsAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new List<TopAlarm>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "dbo.usp_ReportTopAlarms", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Priority",
            (object?)filter.Priority ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceServer",
            string.IsNullOrWhiteSpace(filter.SourceServer)
                ? DBNull.Value : filter.SourceServer);
        command.Parameters.AddWithValue("@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@TopN",
            filter.TopN < 1 ? 20 : filter.TopN);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var pIdx = reader.GetOrdinal("Priority");

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TopAlarm
            {
                AlarmName = reader.GetString(reader.GetOrdinal("AlarmName")),
                TotalCount = reader.GetInt64(reader.GetOrdinal("TotalCount")),
                FirstOccurrence = DateTime.SpecifyKind(
                    reader.GetDateTime(reader.GetOrdinal("FirstOccurrence")),
                    DateTimeKind.Utc),
                LastOccurrence = DateTime.SpecifyKind(
                    reader.GetDateTime(reader.GetOrdinal("LastOccurrence")),
                    DateTimeKind.Utc),
                Priority = reader.IsDBNull(pIdx) ? null : reader.GetInt32(pIdx)
            });
        }

        return result;
    }

    public async Task<List<PriorityAnalysis>> GetPriorityAnalysisAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new List<PriorityAnalysis>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "dbo.usp_ReportPriorityAnalysis", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@SourceServer",
            string.IsNullOrWhiteSpace(filter.SourceServer)
                ? DBNull.Value : filter.SourceServer);
        command.Parameters.AddWithValue("@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var pIdx = reader.GetOrdinal("Priority");

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new PriorityAnalysis
            {
                Priority = reader.IsDBNull(pIdx) ? null : reader.GetInt32(pIdx),
                TotalCount = reader.GetInt64(reader.GetOrdinal("TotalCount")),
                Percentage = reader.GetDecimal(reader.GetOrdinal("Percentage")),
                Srv01Count = reader.GetInt64(reader.GetOrdinal("Srv01Count")),
                Srv02Count = reader.GetInt64(reader.GetOrdinal("Srv02Count"))
            });
        }

        return result;
    }

    public async Task<List<ServerAnalysis>> GetServerAnalysisAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ServerAnalysis>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "dbo.usp_ReportServerAnalysis", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Priority",
            (object?)filter.Priority ?? DBNull.Value);
        command.Parameters.AddWithValue("@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ServerAnalysis
            {
                SourceServer = reader.GetString(reader.GetOrdinal("SourceServer")),
                TotalCount = reader.GetInt64(reader.GetOrdinal("TotalCount")),
                HighPriorityCount = reader.GetInt64(reader.GetOrdinal("HighPriorityCount")),
                Priority1Count = reader.GetInt64(reader.GetOrdinal("Priority1Count")),
                Priority4Count = reader.GetInt64(reader.GetOrdinal("Priority4Count")),
                Priority12Count = reader.GetInt64(reader.GetOrdinal("Priority12Count")),
                FirstOccurrence = DateTime.SpecifyKind(
                    reader.GetDateTime(reader.GetOrdinal("FirstOccurrence")),
                    DateTimeKind.Utc),
                LastOccurrence = DateTime.SpecifyKind(
                    reader.GetDateTime(reader.GetOrdinal("LastOccurrence")),
                    DateTimeKind.Utc)
            });
        }

        return result;
    }

    public async Task<PagedResult<AlarmEvent>> GetCategoryEventsAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = new PagedResult<AlarmEvent>
        {
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "dbo.usp_ReportCategoryEvents", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Category",
            (object?)filter.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@PageNumber", filter.Page);
        command.Parameters.AddWithValue("@PageSize", filter.PageSize);

        command.Parameters.Add("@Search", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(filter.Search)
                ? DBNull.Value : filter.Search.Trim();

        command.Parameters.AddWithValue("@Priority",
            (object?)filter.Priority ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceServer",
            string.IsNullOrWhiteSpace(filter.SourceServer)
                ? DBNull.Value : filter.SourceServer);
        command.Parameters.AddWithValue("@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@Export", false);

        await connection.OpenAsync(cancellationToken);
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

    public async Task<List<AlarmEvent>> GetCategoryEventsForExportAsync(
        AlarmReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var alarms = new List<AlarmEvent>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "dbo.usp_ReportCategoryEvents", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Category",
            (object?)filter.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@PageNumber", 1);
        command.Parameters.AddWithValue("@PageSize", 500);

        command.Parameters.Add("@Search", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(filter.Search)
                ? DBNull.Value : filter.Search.Trim();

        command.Parameters.AddWithValue("@Priority",
            (object?)filter.Priority ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceServer",
            string.IsNullOrWhiteSpace(filter.SourceServer)
                ? DBNull.Value : filter.SourceServer);
        command.Parameters.AddWithValue("@StartDate",
            (object?)filter.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate",
            (object?)filter.EndDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@Export", true);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            alarms.Add(MapAlarm(reader));

        return alarms;
    }
}