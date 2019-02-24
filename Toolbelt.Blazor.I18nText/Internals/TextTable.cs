using System;
using System.Reflection;
using System.Threading.Tasks;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal delegate Task<object> FetchTextTableAsync();

    internal class TextTable
    {
        public readonly Type TableType;

        private readonly Task<object> FetchTextTableTask;

        private readonly FetchTextTableAsync FetchTextTableAsync;

        public TextTable(Type tableType, FetchTextTableAsync fetchTextTableAsync)
        {
            TableType = tableType;
            FetchTextTableAsync = fetchTextTableAsync;
            FetchTextTableTask = fetchTextTableAsync();
        }

        public async Task<T> GetTableAsync<T>() where T : class, I18nTextFallbackLanguage, new()
        {
            return (await FetchTextTableTask) as T;
        }

        public async Task RefreshTableAsync()
        {
            var table = await FetchTextTableTask;
            var newTable = await FetchTextTableAsync();
            var fields = TableType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields) field.SetValue(table, field.GetValue(newTable));
        }
    }
}
