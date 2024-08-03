using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SampleSite.Client.Services;
using SampleSite.Components;
using SampleSite.Components.Services;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Services
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddI18nText(options =>
    {
        // options.PersistanceLevel = PersistanceLevel.Session;
    })
    .AddScoped<IWeatherForecastService, WeatherForecastService>();

await builder.Build().RunAsync();
