using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunnerExecute;

public class ShellRunnerHttp(string host, string path, int port) : IShellRunnerHttp
{
    private readonly HttpClient _httpClient = new();
    public string Host { get; init; } = host + port;
    public string Path { get; init; } = path;

    public async Task<CommandResponse> Execute(CommandRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(Host + Path, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<CommandResponse>(responseJson);
    }
}