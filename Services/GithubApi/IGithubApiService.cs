namespace RuzenBot.Services.GithubApi;

public interface IGithubApiService
{
    Task<string> GetRepoData(string url, CancellationToken cancellationToken);
    Task<string> GetUserData(string url, CancellationToken cancellationToken);
}