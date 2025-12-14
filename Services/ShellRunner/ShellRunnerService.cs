using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunner;

public class ShellRunnerService(ILogger<ShellRunnerService> logger, HttpClient httpClient) : IShellRunnerService
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const string BaseUrl = "http://shellrunner:8081/api";

    public async Task<QueryShellResponse> Execute(string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return new QueryShellResponse("Command cannot be empty", "", 1);
        }

        try
        {
            var request = new QueryShellRequest(command);
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(BaseUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("HTTP Error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<QueryShellResponse>(responseJson, _options);

            return result!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP Request Error executing command");
            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Request timeout executing command");
            return new QueryShellResponse("Request timeout", "", 2);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing command");
            throw;
        }
    }
}
