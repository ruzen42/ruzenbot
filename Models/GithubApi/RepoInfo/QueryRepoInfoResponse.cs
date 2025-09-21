namespace RuzenBot.Models.GithubApi.RepoInfo;

public readonly record struct QueryRepoInfoResponse(
    string Username,
    string RepoName,
    string Description,
    string DataCreated,
    string License,
    int Stars = 0,
    int Issues = 0,
    string Language = null!,
    List<string> Tags = null!)
{
    public override string ToString() => $"\tOwner:{Username}" +
                                         $"\n\tDescription:{Description}" +
                                         $"\n\tRepo:{RepoName}" +
                                         $"\n\tStars:{Stars}" +
                                         $"\n\tIssues:{Issues}" +
                                         $"\n\tLanguage:{Language}" +
                                         $"\n\tData Created:{DataCreated}" +
                                         $"\n\tTags:{Tags.Count}" +
                                         $"\n\tLicense:{License}";
}