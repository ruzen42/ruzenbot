namespace RuzenBot.Models.GithubApi;

public record QueryUserInfoRequest(string Url);

public record QueryUserInfoResponse(
    string Username,
    string Bio,
    string Email,
    string DataCreated,
    int ReposCount = 0,
    int Followers = 0,
    List<string> Tags = null!
);

public record QueryRepoInfoRequest(string Url);

public record QueryRepoInfoResponse(
    string Username,
    string RepoName,
    string Description,
    string DataCreated,
    string License,
    int Stars = 0,
    int Issues = 0,
    string Language = null!,
    List<string> Tags = null!
);