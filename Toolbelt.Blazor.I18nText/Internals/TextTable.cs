using System;
using System.Threading.Tasks;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal delegate ValueTask FetchTextTableAsync(string langCode, object table);

    internal class TextTable
    {
        private readonly FetchTextTableAsync FetchTextTableAsync;

        internal ValueTask FetchTask;

        internal readonly object TableObject;

        public TextTable(Type tableType, string langCode, FetchTextTableAsync fetchTextTableAsync)
        {
            this.TableObject = Activator.CreateInstance(tableType);
            this.FetchTextTableAsync = fetchTextTableAsync;
            this.FetchTask = fetchTextTableAsync(langCode, this.TableObject);
        }

        public async Task<T> GetTableAsync<T>() where T : class, I18nTextFallbackLanguage, new()
        {
            var fetchTask = this.FetchTask;
            await fetchTask;
            return this.TableObject as T;
        }

        public async Task RefreshTableAsync(string langCode)
        {
            var fetchTask = this.FetchTask;
            await fetchTask;
            fetchTask = this.FetchTextTableAsync(langCode, this.TableObject);
            this.FetchTask = fetchTask;
            await fetchTask;
        }
    }
}
