using Microsoft.AspNetCore.Localization;
using SampleSite.Components.Services;
using SampleSite.Server.Components;
using SampleSite.Server.Data;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddI18nText(options => options.PersistenceLevel = PersistanceLevel.PersistentCookie);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseHttpsRedirection();
app.UseAntiforgery();
#if NET9_0_OR_GREATER
app.MapStaticAssets();
#else
app.UseStaticFiles();
#endif

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(SampleSite.Components.App).Assembly);

app.Run();
