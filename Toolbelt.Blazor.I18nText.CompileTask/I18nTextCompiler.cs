using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nTextCompiler
    {
        public bool Compile(IEnumerable<I18nTextSourceFile> srcFiles, I18nTextCompilerOptions options)
        {
            try
            {
                var i18textSrc = ParseSourceFiles(srcFiles, options);
                OutputTypesFiles(options, i18textSrc);
                OutputI18nTextJsonFiles(options, i18textSrc);
                return true;
            }
            catch (AggregateException e) when (e.InnerException is I18nTextCompileException compileException)
            {
                options.LogError(compileException.Message);
                return false;
            }
            catch (I18nTextCompileException compileException)
            {
                options.LogError(compileException.Message);
                return false;
            }
        }

        private static I18nTextSource ParseSourceFiles(IEnumerable<I18nTextSourceFile> srcFiles, I18nTextCompilerOptions options)
        {
            var i18textSrc = new I18nTextSource();
            if (!srcFiles.Any()) return i18textSrc;

            Parallel.ForEach(srcFiles, srcFile =>
            {
                var fnameParts = Path.GetFileNameWithoutExtension(srcFile.Path).Split('.');
                var typeName = string.Join(".", fnameParts.Take(fnameParts.Length - 1));
                var langCode = fnameParts.Last();
                var srcText = File.ReadAllText(srcFile.Path, srcFile.Encoding);
                var textTable = DeserializeSrcText(srcText);

                var type = i18textSrc.Types.GetOrAdd(typeName, new I18nTextType());
                type.Langs[langCode] = textTable;
            });

            Parallel.ForEach(i18textSrc.Types.Values, type =>
            {
                type.TextKeys = type.Langs
                    .SelectMany(lang => lang.Value)
                    .Select(tt => tt.Key)
                    .OrderBy(key => key)
                    .Distinct()
                    .ToList();

                Parallel.ForEach(type.Langs, lang =>
                {
                    var textTable = lang.Value;
                    foreach (var textKey in type.TextKeys.Where(k => !textTable.ContainsKey(k)))
                    {
                        var text = type.Langs.Keys
                            .OrderBy(langCode => langCode.StartsWith("en") ? "0" : langCode)
                            .Select(langCode => type.Langs[langCode].TryGetValue(textKey, out var t) ? t : null)
                            .FirstOrDefault(t => t != null);
                        textTable[textKey] = text ?? textKey;
                    }
                });
            });

            return i18textSrc;
        }

        private static I18nTextTable DeserializeSrcText(string srcText)
        {
            // NOTE:
            // a JSON.NET old version has problem that it can't deserialize ConcurrentDictionary directly.
            // Therefore, deserialize into normal dictionary at first, and second, re - constrauct as ConcurrentDictionary.
            var tableTextRaw = JsonConvert.DeserializeObject<Dictionary<string, string>>(srcText);
            return new I18nTextTable(tableTextRaw);
        }

        private static void OutputTypesFiles(I18nTextCompilerOptions options, I18nTextSource i18textSrc)
        {
            if (!i18textSrc.Types.Any()) return;
            if (!Directory.Exists(options.TypesDirectory)) Directory.CreateDirectory(options.TypesDirectory);

            Parallel.ForEach(i18textSrc.Types, type =>
            {
                var typeFullName = options.NameSpace + "." + type.Key;
                var typeNameParts = typeFullName.Split('.');
                var typeNamespace = string.Join(".", typeNameParts.Take(typeNameParts.Length - 1));
                var typeName = typeNameParts.Last();

                var typeFilePath = Path.Combine(options.TypesDirectory, typeFullName + ".cs");

                var langs = type.Value.Langs;
                var langParts = options.FallBackLanguage.Split('-');
                var fallbackLangs = langParts.Length > 1 ? new[] { options.FallBackLanguage, langParts[0] } : new[] { options.FallBackLanguage };
                var fallbackLang = fallbackLangs.FirstOrDefault(lang => langs.ContainsKey(lang));
                if (fallbackLang == null) throw new I18nTextCompileException($"IN1001: Could not find an I18n source text file of fallback language '{options.FallBackLanguage}', for '{options.NameSpace}.{type.Key}'.");
                var textTable = langs[fallbackLang];

                var typeCode = new List<string>();
                typeCode.Add($"namespace {typeNamespace}");
                typeCode.Add("{");
                typeCode.Add($"    public partial class {typeName} : global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextFallbackLanguage, global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextLateBinding");
                typeCode.Add("    {");
                typeCode.Add($"        string global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextFallbackLanguage.FallBackLanguage => \"{options.FallBackLanguage}\";");
                typeCode.Add("");
                typeCode.Add("        public string this[string key] => global::Toolbelt.Blazor.I18nText.I18nTextExtensions.GetFieldValue(this, key);");
                typeCode.Add("");
                var is1stLine = true;
                foreach (var textKey in type.Value.TextKeys)
                {
                    if (!is1stLine) typeCode.Add("");
                    typeCode.Add($"        /// <summary>\"{SecurityElement.Escape(textTable[textKey])}\"</summary>");
                    typeCode.Add($"        public string {textKey};");
                    is1stLine = false;
                }
                typeCode.Add("    }");
                typeCode.Add("}");

                var skipOutput = false;
                if (File.Exists(typeFilePath))
                {
                    var prevTypeCode = File.ReadAllLines(typeFilePath);
                    skipOutput = prevTypeCode.SequenceEqual(typeCode);
                }

                if (!skipOutput) File.WriteAllLines(typeFilePath, typeCode);
            });
        }

        private void OutputI18nTextJsonFiles(I18nTextCompilerOptions options, I18nTextSource i18textSrc)
        {
            if (!i18textSrc.Types.Any()) return;
            if (!Directory.Exists(options.OutDirectory)) Directory.CreateDirectory(options.OutDirectory);

            Parallel.ForEach(i18textSrc.Types, type =>
            {
                Parallel.ForEach(type.Value.Langs, lang =>
                {
                    var jsonPath = Path.Combine(options.OutDirectory, options.NameSpace + "." + type.Key + "." + lang.Key + ".json");

                    var textTable = new SortedDictionary<string, string>(lang.Value);
                    var jsonText = JsonConvert.SerializeObject(textTable, Formatting.Indented);

                    var skipOutput = false;
                    if (File.Exists(jsonPath))
                    {
                        var prevJsonText = File.ReadAllText(jsonPath);
                        skipOutput = prevJsonText == jsonText;
                    }

                    if (!skipOutput) File.WriteAllText(jsonPath, jsonText);
                });
            });
        }
    }
}
