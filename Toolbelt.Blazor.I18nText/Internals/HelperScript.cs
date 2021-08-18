using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class HelperScript
#if ENABLE_JSMODULE
        : IAsyncDisposable
#endif
    {
        private delegate ValueTask<T> ScriptInvoker<T>(string identifier, params object[] args);

        private bool ScriptLoaded = false;

        private readonly SemaphoreSlim Syncer = new SemaphoreSlim(1, 1);

        private readonly IJSRuntime JSRuntime;

        private static readonly string Namespace = "Toolbelt.Blazor.I18nText";

        public HelperScript(IJSRuntime jSRuntime)
        {
            this.JSRuntime = jSRuntime;
        }

        private string GetVersionText()
        {
            var assembly = this.GetType().Assembly;
            var version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? assembly.GetName().Version!.ToString();
            return version;
        }

#if ENABLE_JSMODULE
        private IJSObjectReference? JSModule = null;

        private async ValueTask<ScriptInvoker<T>?> EnsureScriptEnabledAsync<T>()
        {
            if (!this.ScriptLoaded)
            {
                await this.Syncer.WaitAsync();
                try
                {
                    if (!this.ScriptLoaded)
                    {
                        var version = this.GetVersionText();
                        var scriptPath = "./_content/" + Namespace + "/script.module.min.js?v=" + version;
                        this.JSModule = await this.JSRuntime.InvokeAsync<IJSObjectReference>("import", scriptPath);
                        this.ScriptLoaded = true;
                    }
                }
                catch { }
                finally { this.Syncer.Release(); }
            }

            return this.JSModule == null ? default(ScriptInvoker<T>) : this.JSModule.InvokeAsync<T>;
        }
#else
        private async ValueTask<ScriptInvoker<T>?> EnsureScriptEnabledAsync<T>()
        {
            if (!this.ScriptLoaded)
            {
                await this.Syncer.WaitAsync();
                try
                {
                    if (!this.ScriptLoaded)
                    {
                        var version = this.GetVersionText();
                        var scriptPath = "./_content/" + Namespace + "/script.min.js";
                        await this.JSRuntime.InvokeVoidAsync("eval", "new Promise(r=>((d,t,s,v)=>(h=>h.querySelector(t+`[src^=\"${s}\"]`)?r():(e=>(e.src=(s+v),e.onload=r,h.appendChild(e)))(d.createElement(t)))(d.head))(document,'script','" + scriptPath + "','?v=" + version + "'))");
                        await this.JSRuntime.InvokeVoidAsync("eval", "Toolbelt.Blazor.I18nText.ready");
                        this.ScriptLoaded = true;
                    }
                }
                catch { }
                finally { this.Syncer.Release(); }
            }
            return this.ScriptLoaded == false ? default(ScriptInvoker<T>) : this.JSRuntime.InvokeAsync<T>;
        }
#endif

        internal async ValueTask<IJSRuntime> GetJSRuntimeAsync()
        {
            if (!this.ScriptLoaded)
            {
                await this.Syncer.WaitAsync();
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
                finally { this.Syncer.Release(); }
            }
            return this.JSRuntime;
        }

        internal static async ValueTask<string> DefaultGetInitialLanguageAsync(IServiceProvider serviceProvider, I18nTextOptions options)
        {
            var helperScript = serviceProvider.GetRequiredService<HelperScript>();
            var invoker = await helperScript.EnsureScriptEnabledAsync<string>();
            if (invoker == null) return "en-US";
            return await invoker.Invoke(Namespace + ".initLang", options.PersistanceLevel);
        }

        internal static async ValueTask DefaultPersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options)
        {
            var helperScript = serviceProvider.GetRequiredService<HelperScript>();
            var invoker = await helperScript.EnsureScriptEnabledAsync<object>();
            if (invoker == null) return;
            await invoker.Invoke(Namespace + ".setCurrentLang", langCode, options.PersistanceLevel);
        }

#if ENABLE_JSMODULE
        public async ValueTask DisposeAsync()
        {
            if (this.JSModule != null) { await this.JSModule.DisposeAsync(); }
        }
#endif
    }
}
