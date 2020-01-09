using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using SampleSite.Client.Services;
using SampleSite.Components;
using SampleSite.Components.Services;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace SampleSite.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddI18nText();
            services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
