using System.Net.Http.Json;
using SampleSite.Components.Services;

namespace SampleSite.Client.Services;

public class WeatherForecastService : IWeatherForecastService
{
    private readonly HttpClient HttpClient;

    public WeatherForecastService(HttpClient httpClient)
    {
        this.HttpClient = httpClient;
    }

    public async Task<WeatherForecast[]> GetForecastAsync()
    {
        return await this.HttpClient.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json") ?? Array.Empty<WeatherForecast>();
    }
}
