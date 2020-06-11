using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class HelperScript
    {
        private bool ScriptLoaded = false;

        private readonly SemaphoreSlim Syncer = new SemaphoreSlim(1, 1);

        private readonly IJSRuntime JSRuntime;

        private static readonly string Namespace = "Toolbelt.Blazor.I18nText";

        public HelperScript(IJSRuntime jSRuntime)
        {
            JSRuntime = jSRuntime;
        }

        internal async ValueTask<IJSRuntime> GetJSRuntimeAsync()
        {
            if (!this.ScriptLoaded)
            {
                await Syncer.WaitAsync();
                try
                {
                    if (!this.ScriptLoaded)
                    {
                        var scriptPath = "_content/" + Namespace + "/script.min.js";
                        await this.JSRuntime.InvokeVoidAsync("eval", "new Promise(r=>((d,t,s)=>(h=>h.querySelector(t+`[src=\"${{s}}\"]`)?r():(e=>(e.src=s,e.onload=r,h.appendChild(e)))(d.createElement(t)))(d.head))(document,'script','" + scriptPath + "'))");
                        this.ScriptLoaded = true;
                    }
                }
                catch (Exception) { }
                finally { Syncer.Release(); }
            }
            return this.JSRuntime;
        }

        internal static async ValueTask<string> DefaultGetInitialLanguageAsync(IServiceProvider serviceProvider, I18nTextOptions options)
        {
            var helperScript = serviceProvider.GetRequiredService<HelperScript>();
            var jsRuntime = await helperScript.GetJSRuntimeAsync();
            return await jsRuntime.InvokeAsync<string>(Namespace + ".initLang", options.PersistanceLevel);
        }

        internal static async ValueTask DefaultPersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options)
        {
            var helperScript = serviceProvider.GetRequiredService<HelperScript>();
            var jsRuntime = await helperScript.GetJSRuntimeAsync();
            await jsRuntime.InvokeVoidAsync(Namespace + ".setCurrentLang", langCode, options.PersistanceLevel);
        }
    }
}
