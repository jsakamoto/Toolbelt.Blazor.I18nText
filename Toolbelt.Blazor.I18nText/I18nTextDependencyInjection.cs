﻿using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Toolbelt.Blazor.I18nText;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding I18n Text service.
/// </summary>
public static class I18nTextDependencyInjection
{
    /// <summary>
    ///  Please use AddI18nText() (non generic/without type parameter version) instead.
    /// </summary>
    /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
    /// <param name="configure"></param>
    [Obsolete("Please use AddI18nText() (non generic/without type parameter version) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddI18nText<TStartup>(this IServiceCollection services, Action<I18nTextOptions>? configure = null) where TStartup : class
    {
        return services.AddI18nText(configure);
    }

    /// <summary>
    ///  Adds a I18n Text service to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
    /// <param name="configure"></param>
    public static IServiceCollection AddI18nText(this IServiceCollection services, Action<I18nTextOptions>? configure = null)
    {
        var options = new I18nTextOptions
        {
            GetInitialLanguageAsync = HelperScript.DefaultGetInitialLanguageAsync,
            PersistCurrentLanguageAsync = HelperScript.DefaultPersistCurrentLanguageAsync,
            ConfigureHttpClient = DefaultConfigureHttpClient
        };
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.Configure<LoggerFilterOptions>(logFilterOptions =>
        {
            logFilterOptions.AddFilter("System.Net.Http.HttpClient." + I18nTextOptions.DefaultHttpClientName, options.HttpClientLogLevel);
        });

        // for Blazor WebAssembly apps
        if (OperatingSystem.IsBrowser())
        {
            if (options.ConfigureHttpClient != null)
            {
                services.AddHttpClient(options.HttpClientName ?? I18nTextOptions.DefaultHttpClientName, (sp, client) => options.ConfigureHttpClient(sp, client));
            }

            services.TryAddScoped<ITextMapReader, TextMapReaderForWasm>();
            services.TryAddScoped(serviceProvider => new I18nTextRepository(serviceProvider.GetRequiredService<ITextMapReader>()));
        }

        // for Blazor Server apps
        else
        {
            services.TryAddSingleton<ITextMapReader, TextMapReaderForServer>();
            services.TryAddSingleton(serviceProvider => new I18nTextRepository(serviceProvider.GetRequiredService<ITextMapReader>()));
        }

        services.TryAddScoped<HelperScript>();
        services.TryAddScoped(serviceProvider => new I18nText.I18nText(serviceProvider));

        return services;
    }

    private static string? BaseAddress = null;

    private static void DefaultConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        if (BaseAddress == null)
        {
            var navigationManager = serviceProvider.GetService<NavigationManager>();
            if (navigationManager != null)
            {
                BaseAddress = navigationManager.BaseUri;
            }
            if (BaseAddress == null) BaseAddress = "";
        }

        if (BaseAddress != "")
        {
            client.BaseAddress = new Uri(BaseAddress);
        }
    }
}
