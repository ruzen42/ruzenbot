namespace RuzenBot.Models.GithubApi.RepoInfo;

public readonly record struct QueryRepoInfoRequest
{
    public string Url { get; init; } 
}