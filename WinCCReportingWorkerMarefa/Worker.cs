using System.Text.Json;
using Microsoft.Extensions.Options;
using WinCCReportingWorkerMarefa.Models;
using WinCCReportingWorkerMarefa.Services;

namespace WinCCReportingWorkerMarefa;

public class Worker : BackgroundService
{
    private readonly RedundancyService _redundancyService;
    private readonly AlarmDatabaseService _databaseService;
    private readonly SyncSettings _syncSettings;
    private readonly ILogger<Worker> _logger;


private static readonly JsonSerializerOptions JsonOptions =
    new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string LoggedAlarmsQuery = """
    query GetLoggedAlarms(
        $start: Timestamp!,
        $end: Timestamp!,
        $max: Int!
    ) {
        loggedAlarms(
            languages: ["en-US"],
            startTime: $start,
            endTime: $end,
            maxNumberOfResults: $max
        ) {
            name
            raiseTime
            priority
        }
    }
    """;

    public Worker(
        RedundancyService redundancyService,
        AlarmDatabaseService databaseService,
        IOptions<SyncSettings> syncSettings,
        ILogger<Worker> logger)
    {
        _redundancyService = redundancyService;
        _databaseService = databaseService;
        _syncSettings = syncSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            """
        WinCC Alarm Reporting Worker started.
        Interval: {IntervalSeconds} seconds
        Overlap: {OverlapSeconds} seconds
        Initial Fetch: {InitialFetchMinutes} minutes
        Max Alarms: {MaxAlarms}
        """,
            _syncSettings.IntervalSeconds,
            _syncSettings.OverlapSeconds,
            _syncSettings.InitialFetchMinutes,
            _syncSettings.MaxAlarmsPerRequest);

        // =====================================
        // RUN IMMEDIATELY ON STARTUP
        // =====================================

        while (!stoppingToken.IsCancellationRequested)
        {
            var fetchStartedAt = DateTime.UtcNow;

            try
            {
                await FetchAndSaveAlarmsAsync(
                    fetchStartedAt,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while synchronizing WinCC alarms.");

                try
                {
                    await _databaseService.UpdateSyncFailureAsync(
                        fetchStartedAt,
                        ex.ToString(),
                        stoppingToken);
                }
                catch (Exception databaseEx)
                {
                    _logger.LogError(
                        databaseEx,
                        "Could not update failed sync state.");
                }
            }

            // =====================================
            // WAIT FOR CONFIGURED INTERVAL
            // =====================================

            _logger.LogInformation(
                "Waiting {IntervalSeconds} seconds before next synchronization.",
                _syncSettings.IntervalSeconds);

            await Task.Delay(
                TimeSpan.FromSeconds(
                    _syncSettings.IntervalSeconds),
                stoppingToken);
        }
    }

    private async Task FetchAndSaveAlarmsAsync(
        DateTime fetchStartedAt,
        CancellationToken cancellationToken)
    {
        // =====================================
        // GET LAST SUCCESSFUL FETCH TIME
        // =====================================

        var lastSuccessfulFetchTime =
            await _databaseService
                .GetLastSuccessfulFetchTimeAsync(
                    cancellationToken);

        DateTime startTime;

        if (lastSuccessfulFetchTime.HasValue)
        {
            // Configured overlap to avoid missing alarms
            startTime =
                lastSuccessfulFetchTime.Value
                    .AddSeconds(
                        -_syncSettings.OverlapSeconds);
        }
        else
        {
            // FIRST RUN:
            // Fetch configured number of previous minutes
            startTime =
                fetchStartedAt
                    .AddMinutes(
                        -_syncSettings.InitialFetchMinutes);
        }

        var endTime = fetchStartedAt;

        _logger.LogInformation(
            "Fetching WinCC alarms from {StartTime} to {EndTime}",
            startTime,
            endTime);

        // =====================================
        // GRAPHQL VARIABLES
        // =====================================

        var variables = new
        {
            start = startTime.ToString(
                "yyyy-MM-ddTHH:mm:ss.fffZ"),

            end = endTime.ToString(
                "yyyy-MM-ddTHH:mm:ss.fffZ"),

            max = _syncSettings.MaxAlarmsPerRequest
        };

        // =====================================
        // FETCH FROM ACTIVE WINCC SERVER
        // =====================================

        var result =
            await _redundancyService.ExecuteQueryAsync(
                LoggedAlarmsQuery,
                variables,
                cancellationToken);

        _logger.LogInformation(
            "GraphQL response received from {Server}",
            result.SourceServer);

        // =====================================
        // DESERIALIZE RESPONSE
        // =====================================

        var graphQLResponse =
            JsonSerializer.Deserialize<
                LoggedAlarmGraphQLResponse>(
                result.Response,
                JsonOptions)
            ?? throw new InvalidOperationException(
                "Could not deserialize GraphQL response.");

        var alarms =
            graphQLResponse.Data?.LoggedAlarms
            ?? [];

        _logger.LogInformation(
            "Received {AlarmCount} alarms from WinCC.",
            alarms.Count);

        // =====================================
        // INSERT INTO DATABASE
        // =====================================

        var insertedCount = 0;
        var duplicateCount = 0;

        foreach (var alarm in alarms)
        {
            var inserted =
                await _databaseService.InsertAlarmAsync(
                    alarm,
                    result.SourceServer,
                    cancellationToken);

            if (inserted)
            {
                insertedCount++;
            }
            else
            {
                duplicateCount++;
            }
        }

        var fetchCompletedAt = DateTime.UtcNow;

        // =====================================
        // UPDATE SYNC STATE
        // ONLY AFTER EVERYTHING SUCCEEDED
        // =====================================

        await _databaseService.UpdateSyncSuccessAsync(
            fetchStartedAt,
            fetchCompletedAt,
            endTime,
            result.SourceServer,
            cancellationToken);

        _logger.LogInformation(
            """
        WinCC alarm synchronization completed.
        Server: {Server}
        Received: {Received}
        Inserted: {Inserted}
        Duplicates: {Duplicates}
        """,
            result.SourceServer,
            alarms.Count,
            insertedCount,
            duplicateCount);
    }


}
