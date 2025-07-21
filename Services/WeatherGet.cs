using System.Runtime.InteropServices;
using OpenWeatherAPI;

namespace RuzenBot.Services;

public class WeatherGet(string apiKey) : IWeatherGet
{
    public string ApiKey { get; } = apiKey;

    async Task<string> IWeatherGet.GetWeather(string city) =>
        System.Text.Json.JsonSerializer.Serialize(await new OpenWeatherApiClient(ApiKey).QueryAsync(city));
}