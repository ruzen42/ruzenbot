namespace RuzenBot.Models.GithubApi;

public class QueryResponse
{
    public string Username { get; init; }
    public string RepoName { get; init; }
    public int Stars { get; init; }
    public int Issues { get; init; }
    
    public override string ToString() => $"Repository owner: {Username}\nRepo: {RepoName}\nStars: {Stars}\nIssues: {Issues}";
}