using System.Runtime.InteropServices;
using OpenWeatherAPI;

namespace RuzenBot.Services;

public class WeatherGet(string apiKey) : IWeatherGet
{
    public string ApiKey { get; } = apiKey;

    async Task<string> IWeatherGet.GetWeather(string city) => 
        new OpenWeatherApiClient(ApiKey).QueryAsync(city).Result.Base;
}