namespace RuzenBot.Services;

public interface IWeatherGet
{
    public Task<string> GetWeather(string city);    
}