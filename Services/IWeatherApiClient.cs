using OpenWeatherAPI;

namespace RuzenBot.Services;

public interface IWeatherApiClient
{
    Task<QueryResponse> GetWeatherAsync(string city);
}