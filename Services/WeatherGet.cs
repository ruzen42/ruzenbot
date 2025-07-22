using System.Runtime.InteropServices;
using OpenWeatherAPI;

namespace RuzenBot.Services;

public class WeatherGet(IWeatherApiClient weatherClient) : IWeatherGet
{
    public async Task<string> GetWeather(string city)
    {
        var result = await weatherClient.GetWeatherAsync(city);
        return result.Base;
    }
}