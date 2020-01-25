using System;
using System.Threading.Tasks;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal delegate ValueTask FetchTextTableAsync(object table);

    internal class TextTable
    {
        public readonly Type TableType;

        private readonly ValueTask InitialFetchTask;

        private readonly FetchTextTableAsync FetchTextTableAsync;

        public object TableObject;

        public TextTable(Type tableType, FetchTextTableAsync fetchTextTableAsync)
        {
            TableType = tableType;
            this.TableObject = Activator.CreateInstance(tableType);
            this.FetchTextTableAsync = fetchTextTableAsync;
            this.InitialFetchTask = fetchTextTableAsync(this.TableObject);
        }

        public async Task<T> GetTableAsync<T>() where T : class, I18nTextFallbackLanguage, new()
        {
            await this.InitialFetchTask;
            return this.TableObject as T;
        }

        public async Task RefreshTableAsync()
        {
            await this.InitialFetchTask;
            await this.FetchTextTableAsync(this.TableObject);
        }
    }
}
