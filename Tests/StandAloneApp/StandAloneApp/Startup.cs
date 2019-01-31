using System;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText;

namespace StandAloneApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddI18nText<Startup>(option =>
            {
                //option.PersistanceLevel = PersistanceLevel.Session;
            });
        }

        public void Configure(IBlazorApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
