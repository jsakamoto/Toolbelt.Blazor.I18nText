using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText;

public class I18nText : IDisposable
{
    internal readonly I18nTextOptions Options;

    private string _CurrentLanguage = "en";

    private readonly WeakRefCollection<ComponentBase> Components = new();

    private Task? InitLangTask;

    private readonly IServiceProvider ServiceProvider;

    private readonly I18nTextRepository I18nTextRepository;

    private readonly Guid ScopeId = Guid.NewGuid();

    public event EventHandler<I18nTextChangeLanguageEventArgs>? ChangeLanguage;

    internal I18nText(IServiceProvider serviceProvider, I18nTextOptions options)
    {
        this.ServiceProvider = serviceProvider;
        this.I18nTextRepository = serviceProvider.GetRequiredService<I18nTextRepository>();
        this.Options = options;
    }

    internal void InitializeCurrentLanguage()
    {
        var getInitialLanguageAsync = this.Options.GetInitialLanguageAsync ?? HelperScript.DefaultGetInitialLanguageAsync;
        this.InitLangTask = getInitialLanguageAsync.Invoke(this.ServiceProvider, this.Options)
            .AsTask()
            .ContinueWith(t => { this._CurrentLanguage = t.IsFaulted ? CultureInfo.CurrentUICulture.Name : t.Result; });
    }

    public async Task<string> GetCurrentLanguageAsync()
    {
        await this.EnsureInitialLangAsync();
        return this._CurrentLanguage;
    }

    public async Task SetCurrentLanguageAsync(string langCode)
    {
        if (this._CurrentLanguage == langCode) return;

        if (this.Options.PersistCurrentLanguageAsync != null)
        {
            await this.Options.PersistCurrentLanguageAsync.Invoke(this.ServiceProvider, langCode, this.Options);
        }

        this._CurrentLanguage = langCode;
        await this.I18nTextRepository.ChangeLanguageAsync(this.ScopeId, this._CurrentLanguage);
        this.ChangeLanguage?.Invoke(this, new I18nTextChangeLanguageEventArgs(langCode));

        this.Components.InvokeStateHasChanged();
    }

    public async Task<T> GetTextTableAsync<T>(ComponentBase component) where T : class, I18nTextFallbackLanguage, new()
    {
        await this.EnsureInitialLangAsync();
        this.Components.Add(component);
        var textTable = await this.I18nTextRepository.GetTextTableAsync<T>(this.ScopeId, this._CurrentLanguage, singleLangInAScope: true);
        return textTable ?? new T();
    }

    private async Task EnsureInitialLangAsync()
    {
        var initLangTask = default(Task);
        lock (this) initLangTask = this.InitLangTask;
        if (initLangTask != null && !initLangTask.IsCompleted)
        {
            await initLangTask;
            lock (this) { this.InitLangTask?.Dispose(); this.InitLangTask = null; }
        }
    }

    public void Dispose()
    {
        this.I18nTextRepository.RemoveScope(this.ScopeId);
    }
}
