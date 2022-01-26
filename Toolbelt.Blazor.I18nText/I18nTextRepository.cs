using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText
{
    using LangToTextTable = ConcurrentDictionary<string, ConcurrentDictionary<Type, TextTable>>;
    using ScopeToLang = ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentDictionary<Type, TextTable>>>;
    using TypeToTextTable = ConcurrentDictionary<Type, TextTable>;

    internal class I18nTextRepository
    {
        private readonly ScopeToLang ScopeToLangs = new ScopeToLang();

        private readonly HttpClient? HttpClient;

        private readonly ReadJsonAsTextMapAsync ReadJsonAsTextMapAsync;

        public event EventHandler<I18nTextChangeLanguageEventArgs>? ChangeLanguage;

        internal I18nTextRepository(IServiceProvider serviceProvider, I18nTextOptions options)
        {
            var isWasm = options.IsWasm?.Invoke() ?? I18nTextDependencyInjection.IsWasm;
            if (isWasm)
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                this.HttpClient = httpClientFactory.CreateClient(options.HttpClientName);
                this.ReadJsonAsTextMapAsync = this.ReadJsonAsTextMapWasmAsync;
            }
            else
            {
                this.ReadJsonAsTextMapAsync = this.GetReadJsonAsTextMapServerAsync();
            }
        }

        internal async ValueTask<T?> GetTextTableAsync<T>(Guid scopeId, string langCode, bool singleLangInAScope) where T : class, I18nTextFallbackLanguage, new()
        {
            var textTable = GetLazyTextTable(scopeId, langCode, typeof(T), singleLangInAScope);
            if (textTable == null) return null;
            await textTable.FetchTask;
            return textTable.TableObject as T;
        }

        internal TextTable? GetLazyTextTable(Guid scopeId, string langCode, Type typeofTextTable, bool singleLangInAScope)
        {
            if (typeofTextTable == null) return null;

            var langs = this.ScopeToLangs.GetOrAdd(scopeId, new LangToTextTable());
            var typeToTables = langs.GetOrAdd(singleLangInAScope ? "" : langCode, new TypeToTextTable());
            var textTable = typeToTables.GetOrAdd(typeofTextTable, typeofTextTable => CreateTextTable(typeofTextTable, langCode));
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

        private async ValueTask<Dictionary<string, string>?> ReadJsonAsTextMapWasmAsync(string jsonUrl)
        {
            if (this.HttpClient == null) throw new NullReferenceException($"{nameof(I18nTextRepository)}.{nameof(HttpClient)} is null.");
            var httpRes = await this.HttpClient.GetAsync(jsonUrl);
            if (httpRes.StatusCode == HttpStatusCode.NotFound) return null;
            var contentBytes = await httpRes.Content.ReadAsByteArrayAsync();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(contentBytes);
        }

        private ReadJsonAsTextMapAsync GetReadJsonAsTextMapServerAsync()
        {
            var appDomainBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            var baseDir = Path.Combine(appDomainBaseDir, "wwwroot");
            if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar) baseDir += Path.DirectorySeparatorChar;
            var baseUri = new Uri(baseDir);
            return delegate (string jsonUrl) { return this.ReadJsonAsTextMapServerAsync(baseUri, jsonUrl); };
        }

        private ValueTask<Dictionary<string, string>?> ReadJsonAsTextMapServerAsync(Uri baseUri, string jsonUrl)
        {
            var jsonLocalPath = new Uri(baseUri, relativeUri: jsonUrl).LocalPath;
            if (File.Exists(jsonLocalPath))
            {
                var jsonText = File.ReadAllText(jsonLocalPath);
                var textMap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
                return new ValueTask<Dictionary<string, string>?>(textMap);
            }
            return new ValueTask<Dictionary<string, string>?>(default(Dictionary<string, string>));
        }


        private TextTable CreateTextTable(Type typeofTextTable, string langCode)
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
                if (!string.IsNullOrEmpty(textTableHash)) url += "?hash=" + textTableHash;
                jsonUrls.Add(url);
            }

            var textMap = default(Dictionary<string, string>);
            foreach (var jsonUrl in jsonUrls)
            {
                try
                {
                    textMap = await this.ReadJsonAsTextMapAsync(jsonUrl);
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
}
