using RuzenBot.Models.GithubApi;

namespace RuzenBot.Services.GithubApi;

public interface IGithubApiService
{
    Task<QueryRepoInfoResponse> GetRepoData(string url, CancellationToken cancellationToken);
    Task<QueryUserInfoResponse> GetUserData(string url, CancellationToken cancellationToken);
}