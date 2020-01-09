using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText;

namespace Toolbelt.Blazor.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding I18n Text service.
    /// </summary>
    public static class I18nTextDependencyInjection
    {
        /// <summary>
        ///  Please use AddI18nText() (non generic/without type parameter version) instead.
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
        [Obsolete("Please use AddI18nText() (non generic/without type parameter version) instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IServiceCollection AddI18nText<TStartup>(this IServiceCollection services, Action<I18nTextOptions> configure = null) where TStartup : class
        {
            return services.AddI18nText(configure);
        }

        /// <summary>
        ///  Adds a I18n Text service to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
        public static IServiceCollection AddI18nText(this IServiceCollection services, Action<I18nTextOptions> configure = null)
        {
            services.AddScoped(serviceProvider =>
            {
                var i18ntext = new I18nText.I18nText(serviceProvider);
                configure?.Invoke(i18ntext.Options);
                i18ntext.InitializeCurrentLanguage();
                return i18ntext;
            });
            return services;
        }
    }
}
