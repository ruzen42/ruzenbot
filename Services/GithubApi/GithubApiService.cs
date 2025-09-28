using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.GithubApi.RepoInfo;
using RuzenBot.Models.GithubApi.UserInfo;

namespace RuzenBot.Services.GithubApi;

public class GithubApiService(ILogger<GithubApiService> logger) : IGithubApiService
{
    private readonly HttpClient _httpClient = new();
    private const string Url = "http://github-api:8080/api/query/";
    // better is use ENV vars for it

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };
    
    public async Task<QueryRepoInfoResponse> GetRepoData(string url, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(new QueryRepoInfoRequest { Url = url });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(Url + "get-repo", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<QueryRepoInfoResponse>(responseJson, _options);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP Error: {ExMessage}", ex);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError("TaskCanceledException: {Task}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception: {ExMessage}", ex.Message);
        }
        return default;
    }

    public async Task<QueryUserInfoResponse> GetUserData(string url, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(new QueryUserInfoRequest {Url = url});
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(Url + "get-user", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<QueryUserInfoResponse>(responseJson, _options);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP Error: {ExMessage}", ex);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError("TaskCanceledException: {Task}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception: {ExMessage}", ex.Message);
        }
        return default;
    }
}
