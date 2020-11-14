using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SampleSite.Client.Services;
using SampleSite.Components;
using SampleSite.Components.Services;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText;

namespace SampleSite.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.Services
                .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                .AddI18nText(options =>
                {
                    // options.PersistanceLevel = PersistanceLevel.Session;
                })
                .AddScoped<IWeatherForecastService, WeatherForecastService>();

            await builder.Build().RunAsync();
        }
    }
}
