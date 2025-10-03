using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.GithubApi;

namespace RuzenBot.Services.GithubApi;

public class GithubApiService(ILogger<GithubApiService> logger, HttpClient httpClient) : IGithubApiService
{
    private const string BaseUrl = "http://github-api:8080/api/";

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };
    
    public async Task<QueryRepoInfoResponse> GetRepoData(string url, CancellationToken cancellationToken)
    {
        try
        {
            var requestUrl = $"{BaseUrl}repo?Url={Uri.EscapeDataString(url)}";
            
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<QueryRepoInfoResponse>(responseJson, _options)!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP Error: {ExMessage}", ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError("TaskCanceledException: {Task}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception: {ExMessage}", ex.Message);
        }
        return null!;
    }

    public async Task<QueryUserInfoResponse> GetUserData(string url, CancellationToken cancellationToken)
    {
        try
        {
            var requestUrl = $"{BaseUrl}user?Url={Uri.EscapeDataString(url)}";
            
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<QueryUserInfoResponse>(responseJson, _options)!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP Error: {ExMessage}", ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError("TaskCanceledException: {Task}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception: {ExMessage}", ex.Message);
        }
        return null!;
    }
}
