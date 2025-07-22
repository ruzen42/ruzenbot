using OpenWeatherAPI;

namespace RuzenBot.Services;

public class WeatherApiClient(string apiKey) : IWeatherApiClient
{
    private readonly OpenWeatherApiClient _client = new(apiKey);

    public async Task<QueryResponse> GetWeatherAsync(string city) =>
        await _client.QueryAsync(city);
}