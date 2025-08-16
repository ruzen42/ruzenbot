using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.GithubApi;

namespace RuzenBot.Services.GithubApiGetData;

public class GithubApiService(ILogger logger) : IGithubApiService
{
    private readonly HttpClient _httpClient = new();
    private const string Url = "http://githubapi:7070/api/query/getdata";

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };
    
    public async Task<QueryResponse> GetData(QueryRequest request, CancellationToken cancellationToken)
    {   
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(Url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            return JsonSerializer.Deserialize<QueryResponse>(responseJson, _options);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP Error: {ExMessage}", ex);
            return new QueryResponse();
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError("TaskCanceledException: {Task}", ex.Message);
            return new QueryResponse();
        }
        catch (Exception ex)
        {
            logger.LogError("Exception: {ExMessage}", ex.Message);
            return new QueryResponse();
        }
    }
}