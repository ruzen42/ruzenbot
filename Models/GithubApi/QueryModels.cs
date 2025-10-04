namespace RuzenBot.Models.GithubApi;

public record QueryUserInfoResponse(
    string Username,
    string Bio,
    string Email,
    string DataCreated,
    int ReposCount = 0,
    int Followers = 0
)
{
    public override string ToString()
    {
        var email = CheckField(Email, "Email");
        var bio = CheckField(Bio, "Bio");
        return $"{Username}:{bio}{email}\n\tData Created: {DataCreated},\n\tRepos Count: {ReposCount},\n\tFollowers: {Followers}";
    }

    private static string CheckField(string field, string nameField) => 
        string.IsNullOrEmpty(field) ? string.Empty : $"\n\t{nameField}: {field},";
}

public record QueryRepoInfoResponse(
    string Username,
    string RepoName,
    string Description,
    string DataCreated,
    string License,
    int Stars = 0,
    int Issues = 0,
    string Language = null!
)
{
    public override string ToString()
    {
        var desc = CheckField(Description, "Description"); 
        var license = CheckField(License, "License"); 
        return $"{Username}/{RepoName}:{desc}\n\tStars: {Stars},\n\tData Created: {DataCreated},\n\tLanguage: {Language},{license}\n\tIssues: {Issues}";
    }

    private static string CheckField(string field, string nameField) => 
        string.IsNullOrEmpty(field) ? string.Empty : $"\n\t{nameField}: {field},";
}

