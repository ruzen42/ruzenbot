using System.Text;
using RuzenBot.Models.Casino;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RuzenBot.Services.DbService;

public class BotDbService : IBotDbService
{
    private record EmptyResponse;
    private readonly ILogger<BotDbService> _logger;
    private readonly HttpClient _httpClient;

    public BotDbService(ILogger<BotDbService> logger, HttpClient httpClient)
    { 
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://ruzenbot-db:8082/api");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        _logger.LogInformation("Trying to test database");
    }
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };    
    
    public async Task<User?> GetUser(long id, CancellationToken cancellationToken = default)
        => await SendAsync<User?>(HttpMethod.Get, $"{id}", null, cancellationToken);

    public async Task<List<User>?> GetUsers(CancellationToken cancellationToken = default)
        => await SendAsync<List<User>>(HttpMethod.Get, "", null, cancellationToken);
    
    public async Task DeleteUser(long id, CancellationToken cancellationToken = default)
        => await SendAsync<EmptyResponse>(HttpMethod.Delete, $"{id}", null, cancellationToken);
    
    public async Task CreateUser(User user, CancellationToken cancellationToken = default)
        => await SendAsync<EmptyResponse>(HttpMethod.Post, "", user, cancellationToken);
    
    public async Task UpdateUser(User user, CancellationToken cancellationToken = default)
        => await SendAsync<EmptyResponse>(HttpMethod.Put, $"{user.Id}", user, cancellationToken);
    
    private async Task<TResponse?> SendAsync<TResponse>(
        HttpMethod method, 
        string endpoint, 
        object? content = null, 
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error db response {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
            }

            if (typeof(TResponse) == typeof(EmptyResponse))
            {
                return default;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            return string.IsNullOrEmpty(responseJson) ? default : JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Endpoint} was cancelled: Error: {ex}", endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Request to {Endpoint} errored: Error: {ex}", endpoint, ex.Message);
        }
        return default;
    }
}
