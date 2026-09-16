using Microsoft.Data.SqlClient;
using System.Data;
using WinCCReportingWorkerMarefa.Models.GraphQL;

namespace WinCCReportingWorkerMarefa.Services;

public class SqlService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlService> _logger;

    public SqlService(
        IConfiguration configuration,
        ILogger<SqlService> logger)
    {
        _logger = logger;
        _connectionString =
            configuration.GetConnectionString("ReportingDatabase")
            ?? throw new InvalidOperationException(
                "ReportingDatabase connection string is missing.");
    }

    public async Task<int> SaveAlarmsAsync(
        List<LoggedAlarmDto> alarms,
        string sourceServer,
        CancellationToken cancellationToken)
    {
        var insertedCount = 0;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        foreach (var alarm in alarms)
        {
            await using var command =
                new SqlCommand(
                    "dbo.usp_InsertAlarmEvent",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                "@AlarmName",
                SqlDbType.NVarChar,
                500).Value = alarm.Name;

            command.Parameters.Add(
                "@RaiseTime",
                SqlDbType.DateTime2).Value =
                alarm.RaiseTime;

            command.Parameters.Add(
                "@Priority",
                SqlDbType.Int).Value =
                alarm.Priority;

            command.Parameters.Add(
                "@SourceServer",
                SqlDbType.NVarChar,
                100).Value =
                sourceServer;

            var result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (result != null &&
                Convert.ToInt32(result) == 1)
            {
                insertedCount++;
            }
        }

        return insertedCount;
    }
}