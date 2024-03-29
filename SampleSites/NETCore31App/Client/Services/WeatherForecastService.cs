﻿using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NETCore31App.Components.Services;

namespace NETCore31App.Client.Services
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
            return this.HttpClient.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json");
        }
    }
}
