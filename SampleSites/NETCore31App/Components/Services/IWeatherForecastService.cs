using System;
using System.Threading.Tasks;

namespace NETCore31App.Components.Services
{
    public interface IWeatherForecastService
    {
        Task<WeatherForecast[]> GetForecastAsync();
    }
}
