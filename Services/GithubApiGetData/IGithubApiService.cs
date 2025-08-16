using RuzenBot.Models.GithubApi;

namespace RuzenBot.Services.GithubApiGetData;

public interface IGithubApiService
{
    public Task<QueryResponse> GetData(QueryRequest request, CancellationToken cancellationToken);
}