using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NETCore31App.Client.Services;
using NETCore31App.Components;
using NETCore31App.Components.Services;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace NETCore31App.Client
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
