using System.Text.Json;
using Microsoft.Extensions.Options;
using WinCCReportingWorkerMarefa.Models;

namespace WinCCReportingWorkerMarefa.Services;

public class RedundancyService
{
    private readonly GraphQLService _graphQLService;
    private readonly WinCCOptions _options;
    private readonly ILogger<RedundancyService> _logger;


private const string RedundancyStatusQuery = """
    query {
      tagValues(
        names: [
          "@ServerMachineName",
          "@RedundancyState_1",
          "@RedundancyState_2",
          "@ServerMachineName_1",
          "@ServerMachineName_2"
        ]
      ) {
        name
        value {
          value
          timestamp
        }
        error {
          code
          description
        }
      }
    }
    """;

    public RedundancyService(
        GraphQLService graphQLService,
        IOptions<WinCCOptions> options,
        ILogger<RedundancyService> logger)
    {
        _graphQLService = graphQLService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RedundancyResult> ExecuteQueryAsync(
        string query,
        object? variables,
        CancellationToken cancellationToken = default)
    {
        WinCCServerOptions activeServer;

        // ========================================
        // 1. DETECT CURRENT ACTIVE WINCC SERVER
        // ========================================

        try
        {
            activeServer =
                await GetActiveServerAsync(cancellationToken);

            _logger.LogInformation(
                "Active WinCC server detected: {Server}",
                activeServer.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not determine active WinCC server. Starting with Server1.");

            activeServer = _options.Server1;
        }

        // ========================================
        // 2. TRY ACTIVE / SELECTED SERVER
        // ========================================

        try
        {
            return await ExecuteOnServerAsync(
                activeServer,
                query,
                variables,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Server failed: {Server}. Trying the other redundant server.",
                activeServer.Name);
        }

        // ========================================
        // 3. TRY OTHER REDUNDANT SERVER
        // ========================================

        var otherServer = GetOtherServer(activeServer);

        try
        {
            return await ExecuteOnServerAsync(
                otherServer,
                query,
                variables,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Both WinCC redundant servers failed. Server1: {Server1}, Server2: {Server2}",
                _options.Server1.Name,
                _options.Server2.Name);

            throw new InvalidOperationException(
                "Both WinCC redundancy servers are unavailable.",
                ex);
        }
    }

    // ========================================
    // EXECUTE QUERY ON SERVER
    // ========================================

    private async Task<RedundancyResult> ExecuteOnServerAsync(
        WinCCServerOptions server,
        string query,
        object? variables,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending GraphQL query to WinCC server: {Server}",
            server.Name);

        var response =
            await _graphQLService.ExecuteQueryAsync(
                server,
                query,
                variables,
                cancellationToken);

        _logger.LogInformation(
            "Successfully received GraphQL response from: {Server}",
            server.Name);

        return new RedundancyResult
        {
            Response = response,
            SourceServer = server.Name
        };
    }

    // ========================================
    // DETECT ACTIVE SERVER
    // ========================================

    private async Task<WinCCServerOptions> GetActiveServerAsync(
        CancellationToken cancellationToken)
    {
        string response;

        // Try Server1 first only to READ redundancy status.
        // This does NOT mean Server1 is always active.
        try
        {
            _logger.LogInformation(
                "Checking redundancy status using Server1 endpoint: {Server}",
                _options.Server1.Name);

            response =
                await _graphQLService.ExecuteQueryAsync(
                    _options.Server1,
                    RedundancyStatusQuery,
                    null,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not read redundancy status from Server1: {Server}. Trying Server2: {FallbackServer}.",
                _options.Server1.Name,
                _options.Server2.Name);

            response =
                await _graphQLService.ExecuteQueryAsync(
                    _options.Server2,
                    RedundancyStatusQuery,
                    null,
                    cancellationToken);
        }

        var activeMachineName =
            ParseActiveMachineName(response);

        _logger.LogInformation(
            "WinCC reports current active machine as: {MachineName}",
            activeMachineName);

        if (string.Equals(
                activeMachineName,
                _options.Server1.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return _options.Server1;
        }

        if (string.Equals(
                activeMachineName,
                _options.Server2.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return _options.Server2;
        }

        throw new InvalidOperationException(
            $"WinCC active machine '{activeMachineName}' does not match Server1 '{_options.Server1.Name}' or Server2 '{_options.Server2.Name}'.");
    }

    // ========================================
    // PARSE ACTIVE MACHINE NAME
    // ========================================

    private static string ParseActiveMachineName(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty(
                "data",
                out var dataElement))
        {
            throw new InvalidOperationException(
                "WinCC GraphQL response does not contain 'data'.");
        }

        if (!dataElement.TryGetProperty(
                "tagValues",
                out var tagValuesElement))
        {
            throw new InvalidOperationException(
                "WinCC GraphQL response does not contain 'tagValues'.");
        }

        foreach (var tag in tagValuesElement.EnumerateArray())
        {
            if (!tag.TryGetProperty(
                    "name",
                    out var nameElement))
            {
                continue;
            }

            var name = nameElement.GetString();

            if (!string.Equals(
                    name,
                    "@ServerMachineName",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!tag.TryGetProperty(
                    "value",
                    out var valueElement) ||
                valueElement.ValueKind == JsonValueKind.Null)
            {
                throw new InvalidOperationException(
                    "@ServerMachineName has no value.");
            }

            if (!valueElement.TryGetProperty(
                    "value",
                    out var machineNameElement))
            {
                throw new InvalidOperationException(
                    "@ServerMachineName response does not contain the machine name.");
            }

            var machineName =
                machineNameElement.GetString();

            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new InvalidOperationException(
                    "@ServerMachineName returned an empty machine name.");
            }

            return machineName;
        }

        throw new InvalidOperationException(
            "Could not find @ServerMachineName in WinCC GraphQL response.");
    }

    // ========================================
    // GET OTHER REDUNDANT SERVER
    // ========================================

    private WinCCServerOptions GetOtherServer(
        WinCCServerOptions currentServer)
    {
        if (string.Equals(
                currentServer.Name,
                _options.Server1.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return _options.Server2;
        }

        if (string.Equals(
                currentServer.Name,
                _options.Server2.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return _options.Server1;
        }

        throw new InvalidOperationException(
            $"Server '{currentServer.Name}' is not configured as Server1 or Server2.");
    }

}

public class RedundancyResult
{
    public string Response { get; set; } = string.Empty;

public string SourceServer { get; set; } = string.Empty;


}
