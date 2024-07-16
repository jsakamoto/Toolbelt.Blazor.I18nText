using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Toolbelt.Blazor.I18nText.Internals;

internal class HelperScript : IAsyncDisposable
{
    private delegate ValueTask<T> ScriptInvoker<T>(string identifier, params object[] args);

    private bool ScriptLoaded = false;

    private readonly SemaphoreSlim Syncer = new(1, 1);

    private readonly IJSRuntime JSRuntime;

    private readonly ILogger<HelperScript> _Logger;

    private static readonly string Namespace = "Toolbelt.Blazor.I18nText";

    private bool? _IsOnline = null;

    private static bool ShowDeveloperGuide() => true;

    public HelperScript(IJSRuntime jSRuntime, ILogger<HelperScript> logger)
    {
        this.JSRuntime = jSRuntime;
        this._Logger = logger;
    }

    private IJSObjectReference? JSModule = null;

    private async ValueTask<ScriptInvoker<T>?> EnsureScriptEnabledAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        if (!this.ScriptLoaded)
        {
            await this.Syncer.WaitAsync();
            try
            {
                if (!this.ScriptLoaded)
                {
                    var scriptPath = "./_content/" + Namespace + "/script.module.min.js";//?v=" + version;

                    // Add version string for refresh token only navigator is online.
                    // (If the app runs on the offline mode, the module url with query parameters might cause the "resource not found" error.)
                    try
                    {
                        this._IsOnline = await this.JSRuntime.InvokeAsync<bool>("Toolbelt.Blazor.getProperty", "navigator.onLine");

                        if (this._IsOnline == true) scriptPath += "?v=" + VersionInfo.VersionText;
                    }
                    catch (JSException e)
                    {
                        if (ShowDeveloperGuide())
                        {
                            if (e.Message.StartsWith("Could not find 'Toolbelt.Blazor.getProperty'"))
                            {
                                this._Logger.LogWarning(e.Message);
                                this._Logger.LogWarning("Please see also: https://github.com/jsakamoto/Toolbelt.Blazor.HotKeys2/issues/17#issuecomment-2195830001");
                            }
                            else this._Logger.LogError(e, e.Message);
                        }
                        else this._Logger.LogError(e, e.Message);
                    }

                    this.JSModule = await this.JSRuntime.InvokeAsync<IJSObjectReference>("import", scriptPath);
                    this.ScriptLoaded = true;
                }
            }
            catch { }
            finally { this.Syncer.Release(); }
        }

        return this.JSModule == null ? default : this.JSModule.InvokeAsync<T>;
    }

    internal static async ValueTask<string> DefaultGetInitialLanguageAsync(IServiceProvider serviceProvider, I18nTextOptions options)
    {
        var helperScript = serviceProvider.GetRequiredService<HelperScript>();
        var invoker = await helperScript.EnsureScriptEnabledAsync<string>();
        if (invoker == null) return CultureInfo.CurrentUICulture.Name;
        return await invoker.Invoke(Namespace + ".initLang", options.PersistanceLevel);
    }

    internal static async ValueTask DefaultPersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options)
    {
        var helperScript = serviceProvider.GetRequiredService<HelperScript>();
        var invoker = await helperScript.EnsureScriptEnabledAsync<object>();
        if (invoker == null) return;
        await invoker.Invoke(Namespace + ".setCurrentLang", langCode, options.PersistanceLevel);
    }

    public async ValueTask<bool> IsOnlineAsync()
    {
        if (!this._IsOnline.HasValue)
        {
            await this.EnsureScriptEnabledAsync<object>();
        }
        return this._IsOnline!.Value;
    }

    public async ValueTask DisposeAsync()
    {
        if (this.JSModule != null) { await this.JSModule.DisposeAsync(); }
    }
}
