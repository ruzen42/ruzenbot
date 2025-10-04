using RuzenBot.Models.GithubApi;

namespace RuzenBot.Services.GithubApi;

public interface IGithubApiService
{
    Task<QueryRepoInfoResponse> GetRepoData(string user, string repo, CancellationToken cancellationToken);
    Task<QueryUserInfoResponse> GetUserData(string user, CancellationToken cancellationToken);
}