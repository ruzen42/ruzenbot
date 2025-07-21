namespace RuzenBot.Services;

public interface IWeatherGet
{
    string ApiKey { get; }
    public Task<string> GetWeather(string city);    
}