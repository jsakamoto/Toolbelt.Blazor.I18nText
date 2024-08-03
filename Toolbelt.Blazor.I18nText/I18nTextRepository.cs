using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText;

using LangToTextTable = ConcurrentDictionary<string, ConcurrentDictionary<Type, TextTable>>;
using ScopeToLang = ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentDictionary<Type, TextTable>>>;
using TypeToTextTable = ConcurrentDictionary<Type, TextTable>;

internal class I18nTextRepository
{
    private readonly ScopeToLang ScopeToLangs = new();

    public event EventHandler<I18nTextChangeLanguageEventArgs>? ChangeLanguage;

    private readonly ITextMapReader _textMapReader;

    internal I18nTextRepository(ITextMapReader textMapReader)
    {
        this._textMapReader = textMapReader;
    }

    internal async ValueTask<T?> GetTextTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        Guid scopeId, string langCode, bool singleLangInAScope) where T : class, I18nTextFallbackLanguage, new()
    {
        var textTable = this.GetLazyTextTable(scopeId, langCode, typeof(T), singleLangInAScope);
        if (textTable == null) return null;
        await textTable.FetchTask;
        return textTable.TableObject as T;
    }

    internal TextTable? GetLazyTextTable(
        Guid scopeId,
        string langCode,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? typeofTextTable,
        bool singleLangInAScope)
    {
        if (typeofTextTable == null) return null;

        var langs = this.ScopeToLangs.GetOrAdd(scopeId, new LangToTextTable());
        var typeToTables = langs.GetOrAdd(singleLangInAScope ? "" : langCode, new TypeToTextTable());
        var textTable = typeToTables.GetOrAdd(
            typeofTextTable,
            ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] typeofTextTable) => this.CreateTextTable(typeofTextTable, langCode));
        return textTable;
    }

    internal async ValueTask ChangeLanguageAsync(Guid scopeId, string newLangCode)
    {
        this.ChangeLanguage?.Invoke(this, new I18nTextChangeLanguageEventArgs(newLangCode));

        var langs = this.ScopeToLangs.GetOrAdd(scopeId, new LangToTextTable());
        if (!langs.TryGetValue("", out var typeToTables)) return;
        var textTables = typeToTables.Values.ToArray();
        var refreshingTasks = textTables.Select(textTable => textTable.RefreshTableAsync(newLangCode));
        await Task.WhenAll(refreshingTasks);
    }

    internal void RemoveScope(Guid scopeId)
    {
        this.ScopeToLangs.TryRemove(scopeId, out var _);
    }

    private TextTable CreateTextTable(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type typeofTextTable,
        string langCode)
    {
        var textTable = new TextTable(typeofTextTable, langCode, this.FetchTextTableAsync);
        return textTable;
    }

    private async ValueTask FetchTextTableAsync(string langCode, object targetTableObject) //where TTextTableObject : class, I18nTextFallbackLanguage, new()
    {
        var fallbackLanguage = (targetTableObject as I18nTextFallbackLanguage)?.FallBackLanguage ?? "en";
        var textTableHash = (targetTableObject as I18nTextTableHash)?.Hash ?? "";
        var typeofTextTableObject = targetTableObject.GetType();

        static string[] splitLangCode(string lang)
        {
            var splitedLang = lang.Split('-');
            return splitedLang.Length == 1 ? new[] { lang } : new[] { lang, splitedLang[0] };
        }
        static void appendLangCode(List<string> target, string[] source) { foreach (var item in source) if (!target.Contains(item)) target.Add(item); }

        var langs = new List<string>(capacity: 4);
        appendLangCode(langs, splitLangCode(langCode));
        appendLangCode(langs, splitLangCode(fallbackLanguage));

        var jsonUrls = new List<string>(langs.Count * 2);
        foreach (var lang in langs)
        {
            var url = "_content/i18ntext/" + typeofTextTableObject.FullName + "." + lang + ".json";
            jsonUrls.Add(url);
        }

        var textMap = default(Dictionary<string, string>);
        foreach (var jsonUrl in jsonUrls)
        {
            try
            {
                textMap = await this._textMapReader.ReadAsync(jsonUrl, textTableHash);
                if (textMap != null) break;
            }
            catch (JsonException) { }
            catch (HttpRequestException e) when (e.Message.Split(' ').Contains("404")) { }
        }

        var fields = typeofTextTableObject.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(f => f.FieldType == typeof(string));
        if (textMap != null)
        {
            foreach (var field in fields)
            {
                field.SetValue(targetTableObject, textMap.TryGetValue(field.Name, out var text) ? text : field.Name);
            }
        }
        else foreach (var field in fields) field.SetValue(targetTableObject, field.Name);
    }
}
