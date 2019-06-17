using System;
using System.Threading.Tasks;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal delegate Task<object> FetchTextTableAsync(object table);

    internal class TextTable
    {
        public readonly Type TableType;

        private readonly Task<object> FetchTextTableTask;

        private readonly FetchTextTableAsync FetchTextTableAsync;

        public TextTable(Type tableType, FetchTextTableAsync fetchTextTableAsync)
        {
            TableType = tableType;
            var table = Activator.CreateInstance(tableType);
            FetchTextTableAsync = fetchTextTableAsync;
            FetchTextTableTask = fetchTextTableAsync(table);
        }

        public async Task<T> GetTableAsync<T>() where T : class, I18nTextFallbackLanguage, new()
        {
            return (await FetchTextTableTask) as T;
        }

        public async Task RefreshTableAsync()
        {
            var table = await FetchTextTableTask;
            await FetchTextTableAsync(table);
        }
    }
}
