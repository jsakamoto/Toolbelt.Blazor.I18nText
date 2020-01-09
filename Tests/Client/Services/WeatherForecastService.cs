using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SampleSite.Components.Services;

namespace SampleSite.Client.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly HttpClient HttpClient;

        public WeatherForecastService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public Task<WeatherForecast[]> GetForecastAsync()
        {
            return this.HttpClient.GetJsonAsync<WeatherForecast[]>("sample-data/weather.json");
        }
    }
}
