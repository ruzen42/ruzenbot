namespace RuzenBot.Models.GithubApi.UserInfo;

public readonly record struct QueryUserInfoResponse(
    string Username,
    string Bio,
    string Email,
    string DataCreated,
    int ReposCount = 0,
    int Followers = 0,
    List<string> Tags = null!
);
  /*  public override string ToString() => $"Username: {Username}\n" +
               $"Bio: {Bio}\n" +
               $"Email: {Email}\n" +
               $"Date Created: {DataCreated}\n" +
               $"Repositories: {ReposCount}\n" +
               $"Followers: {Followers}\n" +
               $"Tags: {string.Join(", ", Tags)}";
        */