using Microsoft.AspNetCore.Localization;
using SampleSite.Components.Services;
using SampleSite.Server.Data;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddI18nText(options => options.PersistenceLevel = PersistanceLevel.PersistentCookie);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IWeatherForecastService, WeatherForecastService>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "ja" };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseHttpsRedirection();
#if NET9_0_OR_GREATER
app.MapStaticAssets();
#else
app.UseStaticFiles();
#endif

app.UseRouting();
#if NET9_0_OR_GREATER
app.MapRazorPages().WithStaticAssets();
#else
app.MapRazorPages();
#endif
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
