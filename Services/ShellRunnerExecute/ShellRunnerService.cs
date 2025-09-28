using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunnerExecute;

public class ShellRunnerService(ILogger<ShellRunnerService> logger) : IShellRunnerService
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };

    private const string Url = "http://shellrunner:8081/api/command/execute";

    public async Task<CommandResponse> Execute(string request, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(new CommandRequest(request));
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(Url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            return JsonSerializer.Deserialize<CommandResponse>(responseJson, _options);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP Error: {ExMessage}", ex);
            return new CommandResponse { Error = ex.Message, ExitCode = ex.HResult };
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError("TaskCanceledException: {Task}", ex.Message);
            return new CommandResponse { Error = "Request timeout", ExitCode = 2};
        }
        catch (Exception ex)
        {
            logger.LogError("Exception: {ExMessage}", ex.Message);
            return new CommandResponse { Error = ex.Message, ExitCode = 1};
        }
    }
}
