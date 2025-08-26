namespace RuzenBot.Models.GithubApi.UserInfo;

public readonly record struct QueryUserInfoResponse()
{
    public string Username { get; init; } = null!;
    public string Bio { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string DataCreated { get; init; } = null!;
    public int ReposCount { get; init; } = 0;
    public int Followers { get; init; } = 0;
    public List<string> Tags { get; init; } = null!;
    
    public override string ToString() => $"Username: {Username}\n" +
               $"Bio: {Bio}\n" +
               $"Email: {Email}\n" +
               $"Date Created: {DataCreated}\n" +
               $"Repositories: {ReposCount}\n" +
               $"Followers: {Followers}\n" +
               $"Tags: {string.Join(", ", Tags)}";
}