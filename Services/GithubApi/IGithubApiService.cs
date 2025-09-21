using RuzenBot.Models.GithubApi.RepoInfo;
using RuzenBot.Models.GithubApi.UserInfo;

namespace RuzenBot.Services.GithubApi;

public interface IGithubApiService
{
    Task<QueryRepoInfoResponse> GetRepoData(string url, CancellationToken cancellationToken);
    Task<QueryUserInfoResponse> GetUserData(string url, CancellationToken cancellationToken);
}