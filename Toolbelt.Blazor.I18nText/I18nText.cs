using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nText : IDisposable
    {
        internal readonly I18nTextOptions Options;

        private string _CurrentLanguage = "en";

        private readonly WeakRefCollection<ComponentBase> Components = new WeakRefCollection<ComponentBase>();

        private Task InitLangTask;

        private readonly IServiceProvider ServiceProvider;

        private readonly I18nTextRepository I18nTextRepository;

        private readonly Guid ScopeId = Guid.NewGuid();

        internal I18nText(IServiceProvider serviceProvider, I18nTextOptions options)
        {
            this.ServiceProvider = serviceProvider;
            this.I18nTextRepository = serviceProvider.GetRequiredService<I18nTextRepository>();
            this.Options = options;
        }

        internal void InitializeCurrentLanguage()
        {
            this.InitLangTask = this.Options.GetInitialLanguageAsync.Invoke(this.ServiceProvider, this.Options)
                .AsTask()
                .ContinueWith(t => { _CurrentLanguage = t.IsFaulted ? CultureInfo.CurrentUICulture.Name : t.Result; });
        }

        public async Task<string> GetCurrentLanguageAsync()
        {
            await EnsureInitialLangAsync();
            return _CurrentLanguage;
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

            this.Components.InvokeStateHasChanged();
        }

        public async Task<T> GetTextTableAsync<T>(ComponentBase component) where T : class, I18nTextFallbackLanguage, new()
        {
            await EnsureInitialLangAsync();
            this.Components.Add(component);
            return await this.I18nTextRepository.GetTextTableAsync<T>(this.ScopeId, this._CurrentLanguage, singleLangInAScope: true);
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
}
