using System;
using System.Threading.Tasks;

namespace SampleSite.Components.Services
{
    public interface IWeatherForecastService
    {
        Task<WeatherForecast[]> GetForecastAsync();
    }
}
