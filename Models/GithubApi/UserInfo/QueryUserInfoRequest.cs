namespace RuzenBot.Models.GithubApi.UserInfo;

public record struct QueryUserInfoRequest
{
    public required string Url {get; set;}
}