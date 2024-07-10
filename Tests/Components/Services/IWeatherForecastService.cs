namespace SampleSite.Components.Services;

public interface IWeatherForecastService
{
    Task<WeatherForecast[]> GetForecastAsync();
}
