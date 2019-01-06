using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.JSInterop;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nText
    {
        private readonly bool RunningOnClientSide;

        private readonly HttpClient HttpClient;

        private readonly Uri WebRootUri;

        private string FallbackLanguage = "en";

        private string _CurrentLanguage = "en";

        private readonly List<TextTable> TextTables = new List<TextTable>();

        private List<WeakReference<BlazorComponent>> Components = new List<WeakReference<BlazorComponent>>();

        private Task InitLangTask;

        public I18nText(Type typeOfStartUp, IServiceProvider serviceProvider)
        {
            this.RunningOnClientSide = RuntimeInformation.OSDescription == "web";
            if (this.RunningOnClientSide)
                this.HttpClient = serviceProvider.GetService(typeof(HttpClient)) as HttpClient;
            else
                this.WebRootUri = GetWebRootUriOnServerSideBlazor(typeOfStartUp);

            var refThis = new DotNetObjectRef(this);
            this.InitLangTask = JSRuntime.Current
                .InvokeAsync<string>("Toolbelt.Blazor.I18nText.initLang", refThis)
                .ContinueWith(_ => refThis.Dispose());
        }

        private static Uri GetWebRootUriOnServerSideBlazor(Type typeOfStartUp)
        {
            var typeOfBlazorConfig = Type.GetType("Microsoft.AspNetCore.Blazor.Server.BlazorConfig, Microsoft.AspNetCore.Blazor.Server");
            if (typeOfBlazorConfig == null) return null;

            var readMethod = typeOfBlazorConfig.GetMethod("Read", BindingFlags.Static | BindingFlags.Public);
            var webRootPathProp = typeOfBlazorConfig.GetProperty("WebRootPath", BindingFlags.Instance | BindingFlags.Public);
            var distPathProp = typeOfBlazorConfig.GetProperty("DistPath", BindingFlags.Instance | BindingFlags.Public);
            if (readMethod == null) return null;
            if (webRootPathProp == null) return null;
            if (distPathProp == null) return null;

            var blazorConfig = readMethod.Invoke(null, new object[] { typeOfStartUp.Assembly.Location });
            if (blazorConfig == null) return null;

            var webRootPath = webRootPathProp.GetValue(blazorConfig, null) as string;
            var distPath = distPathProp.GetValue(blazorConfig, null) as string;

            var webRootDir = new[] { webRootPath, distPath }
                .Where(path => !string.IsNullOrEmpty(path))
                .Where(path => Directory.Exists(path))
                .FirstOrDefault();
            if (string.IsNullOrEmpty(webRootDir)) return null;

            if (!webRootDir.EndsWith(Path.DirectorySeparatorChar.ToString())) webRootDir += Path.DirectorySeparatorChar;

            return new Uri(webRootDir);
        }

        [JSInvokable(nameof(InitLang)), EditorBrowsable(EditorBrowsableState.Never)]
        public void InitLang(string[] langCodes)
        {
            _CurrentLanguage = langCodes.FirstOrDefault() ?? "en";
        }

        public string CurrentLanguage => _CurrentLanguage;

        public async Task SetCurrentLanguageAsync(string langCode)
        {
            if (this._CurrentLanguage == langCode) return;

            this._CurrentLanguage = langCode;
            var allRefreshTasks = this.TextTables.Select(tt => tt.RefreshTableAsync.Invoke(tt.Table));
            await Task.WhenAll(allRefreshTasks);

            SweepGarbageCollectedComponents();
            var stateHasChangedMethod = typeof(BlazorComponent).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var cref in this.Components)
            {
                if (cref.TryGetTarget(out var component))
                {
                    stateHasChangedMethod.Invoke(component, new object[] { });
                }
            }
        }

        public async Task<T> GetTextTableAsync<T>(BlazorComponent component) where T : class, new()
        {
            SweepGarbageCollectedComponents();
            if (!this.Components.Exists(cref => cref.TryGetTarget(out var c) && c == component))
                this.Components.Add(new WeakReference<BlazorComponent>(component));

            var fetchedTextTable = this.TextTables.FirstOrDefault(tt => tt.TableType == typeof(T));
            if (fetchedTextTable != null) return fetchedTextTable.Table as T;

            var table = await FetchTextTableAsync<T>();

            var textTable = new TextTable
            {
                TableType = typeof(T),
                Table = table,
                RefreshTableAsync = (t) => FetchTextTableAsync<T>().ContinueWith(task =>
                {
                    var result = task.Result;
                    var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
                    foreach (var field in fields) field.SetValue(t, field.GetValue(result));
                })
            };
            this.TextTables.Add(textTable);
            return table;
        }

        private void SweepGarbageCollectedComponents()
        {
            // DEBUG: var beforeCount = this.Components.Count;
            this.Components = this.Components.Where(cref => cref.TryGetTarget(out var _)).ToList();
            // DEBUG: var afterCount = this.Components.Count;
            // DEBUG: Console.WriteLine($"SweepGarbageCollectedComponents - {(beforeCount - afterCount)} objects are sweeped. ({this.Components.Count} objects are stay.)");
        }

        private async Task<T> FetchTextTableAsync<T>() where T : class, new()
        {
            var initLangTask = default(Task);
            lock (this) initLangTask = this.InitLangTask;
            if (initLangTask != null && !initLangTask.IsCompleted)
            {
                await initLangTask;
                lock (this) { this.InitLangTask?.Dispose(); this.InitLangTask = null; }
            }

            string[] splitLangCode(string lang)
            {
                var splitedLang = lang.Split('-');
                return splitedLang.Length == 1 ? new[] { lang } : new[] { lang, splitedLang[0] };
            }
            void appendLangCode(List<string> target, string[] source) { foreach (var item in source) if (!target.Contains(item)) target.Add(item); }

            var langs = new List<string>(capacity: 4);
            appendLangCode(langs, splitLangCode(this._CurrentLanguage));
            appendLangCode(langs, splitLangCode(this.FallbackLanguage));

            var table = default(T);
            foreach (var lang in langs)
            {
                try
                {
                    var jsonUrl = "content/i18ntext/" + typeof(T).FullName + "." + lang + ".json";

                    if (this.RunningOnClientSide)
                    {
                        table = await this.HttpClient.GetJsonAsync<T>(jsonUrl);
                        break;
                    }
                    else
                    {
                        if (this.WebRootUri == null) break;

                        var jsonLocalPath = new Uri(baseUri: this.WebRootUri, relativeUri: jsonUrl).LocalPath;
                        if (File.Exists(jsonLocalPath))
                        {
                            var jsonText = File.ReadAllText(jsonLocalPath);
                            table = Json.Deserialize<T>(jsonText);
                            break;
                        }
                    }
                }
                catch (Exception) { }
            }

            if (table == null)
            {
                table = Activator.CreateInstance<T>();
                var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (var field in fields) field.SetValue(table, field.Name);
            }

            return table;
        }

    }
}
