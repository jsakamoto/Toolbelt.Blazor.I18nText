using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText
{
    internal delegate ValueTask<Dictionary<string, string>> ReadJsonAsTextMapAsync(string jsonUrl);

    public class I18nText
    {
        internal readonly I18nTextOptions Options = new I18nTextOptions();

        private readonly HttpClient HttpClient;

        private string _CurrentLanguage = "en";

        private readonly List<TextTable> TextTables = new List<TextTable>();

        private List<WeakReference<ComponentBase>> Components = new List<WeakReference<ComponentBase>>();

        private Task InitLangTask;

        private readonly IServiceProvider ServiceProvider;

        private readonly IJSRuntime JSRuntime;

        private bool ScriptLoaded = false;

        private ReadJsonAsTextMapAsync ReadJsonAsTextMapAsync;

        internal I18nText(IServiceProvider serviceProvider)
        {
            var runningOnWasm = RuntimeInformation.OSDescription == "web";
            if (runningOnWasm)
            {
                this.HttpClient = serviceProvider.GetService(typeof(HttpClient)) as HttpClient;
                this.ReadJsonAsTextMapAsync = this.ReadJsonAsTextMapWasmAsync;
            }
            else
            {
                this.ReadJsonAsTextMapAsync = this.GetReadJsonAsTextMapServerAsync();
            }

            this.ServiceProvider = serviceProvider;
            this.JSRuntime = serviceProvider.GetService<IJSRuntime>();
            this.Options.GetInitialLanguageAsync = GetInitialLanguageAsync;
            this.Options.PersistCurrentLanguageAsync = PersistCurrentLanguageAsync;
        }

        internal void InitializeCurrentLanguage()
        {
            this.InitLangTask = this.Options.GetInitialLanguageAsync.Invoke(this.ServiceProvider, this.Options)
                .AsTask()
                .ContinueWith(t => { _CurrentLanguage = t.IsFaulted ? CultureInfo.CurrentUICulture.Name : t.Result; });
        }

        private readonly SemaphoreSlim Syncer = new SemaphoreSlim(1, 1);

        private async ValueTask<IJSRuntime> GetJSRuntimeAsync()
        {
            if (!this.ScriptLoaded)
            {
                await Syncer.WaitAsync();
                try
                {
                    if (!this.ScriptLoaded)
                    {
                        const string scriptPath = "_content/Toolbelt.Blazor.I18nText/script.min.js";
                        await this.JSRuntime.InvokeVoidAsync("eval", "new Promise(r=>((d,t,s)=>(h=>h.querySelector(t+`[src=\"${{s}}\"]`)?r():(e=>(e.src=s,e.onload=r,h.appendChild(e)))(d.createElement(t)))(d.head))(document,'script','" + scriptPath + "'))");
                        this.ScriptLoaded = true;
                    }
                }
                catch (Exception) { }
                finally { Syncer.Release(); }
            }
            return this.JSRuntime;
        }

        private async ValueTask<string> GetInitialLanguageAsync(IServiceProvider serviceProvider, I18nTextOptions options)
        {
            var jsRuntime = await GetJSRuntimeAsync();
            return await jsRuntime.InvokeAsync<string>("Toolbelt.Blazor.I18nText.initLang", options.PersistanceLevel);
        }

        private async ValueTask PersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options)
        {
            var jsRuntime = await GetJSRuntimeAsync();
            await jsRuntime.InvokeVoidAsync("Toolbelt.Blazor.I18nText.setCurrentLang", langCode, options.PersistanceLevel);
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
            var allRefreshTasks = this.TextTables.Select(tt => tt.RefreshTableAsync());
            await Task.WhenAll(allRefreshTasks);

            SweepGarbageCollectedComponents();
            var stateHasChangedMethod = typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var cref in this.Components)
            {
                if (cref.TryGetTarget(out var component))
                {
                    stateHasChangedMethod.Invoke(component, new object[] { });
                }
            }
        }

        public Task<T> GetTextTableAsync<T>(ComponentBase component) where T : class, I18nTextFallbackLanguage, new()
        {
            SweepGarbageCollectedComponents();
            if (!this.Components.Exists(cref => cref.TryGetTarget(out var c) && c == component))
                this.Components.Add(new WeakReference<ComponentBase>(component));

            var fetchedTextTable = this.TextTables.FirstOrDefault(tt => tt.TableType == typeof(T));
            if (fetchedTextTable == null)
            {
                fetchedTextTable = new TextTable(typeof(T), tableObject => FetchTextTableAsync(tableObject));
                this.TextTables.Add(fetchedTextTable);
            }
            return fetchedTextTable.GetTableAsync<T>();
        }

        public I18nTextLateBinding GetLazyTextTable(Type textTableType)
        {
            var fetchedTextTable = this.TextTables.FirstOrDefault(tt => tt.TableType == textTableType);
            if (fetchedTextTable == null)
            {
                fetchedTextTable = new TextTable(textTableType, tableObject => FetchTextTableAsync(tableObject));
                this.TextTables.Add(fetchedTextTable);
            }
            return fetchedTextTable.TableObject as I18nTextLateBinding;
        }

        private void SweepGarbageCollectedComponents()
        {
            // DEBUG: var beforeCount = this.Components.Count;
            this.Components = this.Components.Where(cref => cref.TryGetTarget(out var _)).ToList();
            // DEBUG: var afterCount = this.Components.Count;
            // DEBUG: Console.WriteLine($"SweepGarbageCollectedComponents - {(beforeCount - afterCount)} objects are sweeped. ({this.Components.Count} objects are stay.)");
        }

        private async ValueTask FetchTextTableAsync(object targetTableObject) //where TTextTableObject : class, I18nTextFallbackLanguage, new()
        {
            await EnsureInitialLangAsync();

            var fallbackLanguage = (targetTableObject as I18nTextFallbackLanguage)?.FallBackLanguage ?? "en";
            var typeofTextTableObject = targetTableObject.GetType();

            static string[] splitLangCode(string lang)
            {
                var splitedLang = lang.Split('-');
                return splitedLang.Length == 1 ? new[] { lang } : new[] { lang, splitedLang[0] };
            }
            static void appendLangCode(List<string> target, string[] source) { foreach (var item in source) if (!target.Contains(item)) target.Add(item); }

            var langs = new List<string>(capacity: 4);
            appendLangCode(langs, splitLangCode(this._CurrentLanguage));
            appendLangCode(langs, splitLangCode(fallbackLanguage));

            var jsonUrls = new List<string>(langs.Count * 2);
            foreach (var lang in langs)
            {
                jsonUrls.Add("_content/i18ntext/" + typeofTextTableObject.FullName + "." + lang + ".json");
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

        private async ValueTask<Dictionary<string, string>> ReadJsonAsTextMapWasmAsync(string jsonUrl)
        {
            var jsonText = await this.HttpClient.GetStringAsync(jsonUrl);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
        }

        private ReadJsonAsTextMapAsync GetReadJsonAsTextMapServerAsync()
        {
            var appDomainBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            var baseDir1 = Path.Combine(appDomainBaseDir, "dist", "_content", "i18ntext");
            var baseDir2 = Path.Combine(appDomainBaseDir, "_content", "i18ntext");
            var baseDir = Directory.Exists(baseDir1) ? Path.Combine(appDomainBaseDir, "dist") : appDomainBaseDir;
            var baseUri = new Uri(baseDir + Path.DirectorySeparatorChar);
            return delegate (string jsonUrl) { return this.ReadJsonAsTextMapServerAsync(baseUri, jsonUrl); };
        }

        private ValueTask<Dictionary<string, string>> ReadJsonAsTextMapServerAsync(Uri baseUri, string jsonUrl)
        {
            var jsonLocalPath = new Uri(baseUri, relativeUri: jsonUrl).LocalPath;
            if (File.Exists(jsonLocalPath))
            {
                var jsonText = File.ReadAllText(jsonLocalPath);
                var textMap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
                return new ValueTask<Dictionary<string, string>>(textMap);
            }
            return new ValueTask<Dictionary<string, string>>(default(Dictionary<string, string>));
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
    }
}
