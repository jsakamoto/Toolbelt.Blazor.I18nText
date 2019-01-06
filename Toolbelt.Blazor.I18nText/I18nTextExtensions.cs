using Microsoft.Extensions.DependencyInjection;

namespace Toolbelt.Blazor.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding I18n Text service.
    /// </summary>
    public static class I18nTextExtensions
    {
        /// <summary>
        ///  Adds a I18n Text service to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
        public static IServiceCollection AddI18nText<TStartup>(this IServiceCollection services) where TStartup : class
        {
            services.AddScoped(serviceProvider => new I18nText.I18nText(typeof(TStartup), serviceProvider));
            return services;
        }
    }
}
