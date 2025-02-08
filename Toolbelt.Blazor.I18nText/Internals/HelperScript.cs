using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Toolbelt.Blazor.I18nText.Internals;

internal class HelperScript : IAsyncDisposable
{
    private delegate ValueTask<T> ScriptInvoker<T>(string identifier, params object[] args);

    private bool _Attached = false;

    private readonly SemaphoreSlim _Syncer = new(1, 1);

    private readonly I18nTextOptions _Options;

    private readonly IJSRuntime _JSRuntime;

    private IJSObjectReference? _JSHelper;

    private readonly DotNetObjectReference<HelperScript> _RefThis;

    private readonly ILogger<HelperScript> _Logger;

    private bool? _IsOnline = null;

    public event AsyncEventHandler<I18nTextChangeLanguageEventArgs>? ChangeLanguage;

    public HelperScript(I18nTextOptions options, IJSRuntime jSRuntime, ILogger<HelperScript> logger)
    {
        this._Options = options;
        this._JSRuntime = jSRuntime;
        this._Logger = logger;
        this._RefThis = DotNetObjectReference.Create(this);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(I18nTextOptions))]
    private async ValueTask<IJSObjectReference?> EnsureScriptAttachedAsync()
    {
        if (!this._Attached)
        {
            await this._Syncer.WaitAsync();
            try
            {
                if (!this._Attached)
                {
                    var scriptPath = "./_content/Toolbelt.Blazor.I18nText/helper.min.js";

                    // Add version string for refresh token only navigator is online.
                    // (If the app runs on the offline mode, the module url with query parameters might cause the "resource not found" error.)
                    try
                    {
                        this._IsOnline = await this._JSRuntime.InvokeAsync<bool>("Toolbelt.Blazor.getProperty", "navigator.onLine");
                        if (this._IsOnline == true) scriptPath += "?v=" + VersionInfo.VersionText;
                    }
                    catch (JSException e)
                    {
                        if (!VersionInfo.IsOptimizedForWasm())
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

                    await using var module = await this._JSRuntime.InvokeAsync<IJSObjectReference>("import", scriptPath);
                    this._JSHelper = await module.InvokeAsync<IJSObjectReference>("attach", this._RefThis, this._Options);
                    this._Attached = true;
                }
            }
            catch { }
            finally { this._Syncer.Release(); }
        }

        return this._JSHelper;
    }

    internal static async ValueTask<string> DefaultGetInitialLanguageAsync(IServiceProvider serviceProvider, I18nTextOptions options)
    {
        var helperScript = serviceProvider.GetRequiredService<HelperScript>();
        var helper = await helperScript.EnsureScriptAttachedAsync();
        if (helper == null) return CultureInfo.CurrentUICulture.Name;
        return await helper.InvokeAsync<string>("getCurrentLang");
    }

    internal static async ValueTask DefaultPersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options)
    {
        var helperScript = serviceProvider.GetRequiredService<HelperScript>();
        var helper = await helperScript.EnsureScriptAttachedAsync();
        if (helper == null) return;
        await helper.InvokeAsync<string>("setCurrentLang", langCode);
    }

    public async ValueTask<bool> IsOnlineAsync()
    {
        if (!this._IsOnline.HasValue)
        {
            await this.EnsureScriptAttachedAsync();
        }
        return this._IsOnline!.Value;
    }

    [JSInvokable(nameof(OnLanguageChanged)), EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnLanguageChanged(string lang)
    {
        await this.ChangeLanguage.InvokeAsync(this, new(lang));
    }

    public async ValueTask DisposeAsync()
    {
        if (this._JSHelper is not null)
        {
            try { await this._JSHelper.InvokeVoidAsync("dispose"); }
            catch (JSDisconnectedException) { }
            try { await this._JSHelper.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
        this._RefThis.Dispose();
    }
}
