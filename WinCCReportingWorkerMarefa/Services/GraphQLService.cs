using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using WinCCReportingWorkerMarefa.Models;

namespace WinCCReportingWorkerMarefa.Services;

public class GraphQLService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphQLService> _logger;

    public GraphQLService(
        HttpClient httpClient,
        ILogger<GraphQLService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> ExecuteQueryAsync(
        WinCCServerOptions server,
        string query,
        object? variables,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(server.Url))
        {
            throw new InvalidOperationException(
                $"URL is not configured for server '{server.Name}'.");
        }

        if (string.IsNullOrWhiteSpace(server.Token))
        {
            throw new InvalidOperationException(
                $"Token is not configured for server '{server.Name}'.");
        }

        var endpoint = server.Url.TrimEnd('/') + "/graphql/";

        if (!Uri.TryCreate(
                endpoint,
                UriKind.Absolute,
                out var endpointUri))
        {
            throw new InvalidOperationException(
                $"Invalid GraphQL endpoint: {endpoint}");
        }

        var payload = new
        {
            query,
            variables
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpointUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                server.Token);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        _logger.LogInformation(
            "Sending GraphQL POST request to {Server}. Endpoint: {Endpoint}",
            server.Name,
            endpoint);

        try
        {
            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            var responseContent =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    """
                    GraphQL request failed.
                    Server: {Server}
                    URL: {Endpoint}
                    HTTP Status: {StatusCode}
                    Response: {Response}
                    """,
                    server.Name,
                    endpoint,
                    (int)response.StatusCode,
                    responseContent);

                throw new HttpRequestException(
                    $"GraphQL request to '{server.Name}' failed. " +
                    $"Status: {(int)response.StatusCode} " +
                    $"({response.ReasonPhrase}). " +
                    $"Response: {responseContent}");
            }

            return responseContent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP/SSL error while connecting to {Server}. Endpoint: {Endpoint}",
                server.Name,
                endpoint);

            throw;
        }
    }
}