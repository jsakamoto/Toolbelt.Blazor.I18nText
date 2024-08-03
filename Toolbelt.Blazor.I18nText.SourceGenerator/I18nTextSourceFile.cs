using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using Toolbelt.Blazor.I18nText.SourceGenerator.Internals;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nTextSourceFile
    {
        public string Path { get; }

        public Encoding Encoding { get; }

        public I18nTextSourceFile(string path, Encoding encoding)
        {
            this.Path = path;
            this.Encoding = encoding;
        }

        public string GetText()
        {
            return File.ReadAllText(this.Path, this.Encoding);
        }

        private delegate string ConvertPath(string srcPath);

        internal static I18nTextSource Parse(IEnumerable<I18nTextSourceFile> srcFiles, I18nTextCompilerOptions options, CancellationToken canceleationToken)
        {
            canceleationToken.ThrowIfCancellationRequested();

            var i18textSrc = new I18nTextSource();
            if (!srcFiles.Any()) return i18textSrc;

            var i18nSrcDir = options.I18nTextSourceDirectory;
            if (!i18nSrcDir.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString())) i18nSrcDir += System.IO.Path.DirectorySeparatorChar;

            ConvertPath convertPath;
            if (options.DisableSubNameSpace)
            {
                convertPath = delegate (string srcPath) { return System.IO.Path.GetFileName(srcPath); };
            }
            else
            {
                convertPath = delegate (string srcPath)
                {
                    return srcPath.StartsWith(i18nSrcDir) ? srcPath.Substring(i18nSrcDir.Length) : System.IO.Path.GetFileName(srcPath);
                };
            }

            Parallel.ForEach(srcFiles, new ParallelOptions { CancellationToken = canceleationToken }, srcFile =>
            {
                var textTable = DeserializeSrcText(srcFile, options);

                var srcName = convertPath(srcFile.Path);
                var fnameParts = srcName.Split('.', System.IO.Path.DirectorySeparatorChar);
                var typeName = string.Join(".", fnameParts.Take(fnameParts.Length - 2));
                var langCode = fnameParts[fnameParts.Length - 2];

                var type = i18textSrc.Types.GetOrAdd(typeName, new I18nTextType());
                type.Langs[langCode] = textTable;
            });

            Parallel.ForEach(i18textSrc.Types.Values, new ParallelOptions { CancellationToken = canceleationToken }, type =>
            {
                type.TextKeys = type.Langs
                    .SelectMany(lang => lang.Value)
                    .Select(tt => tt.Key)
                    .OrderBy(key => key)
                    .Distinct()
                    .ToList();

                Parallel.ForEach(type.Langs, new ParallelOptions { CancellationToken = canceleationToken }, lang =>
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

        private static I18nTextTable DeserializeSrcText(I18nTextSourceFile srcFile, I18nTextCompilerOptions options)
        {
            try
            {
                var fileNameExtension = System.IO.Path.GetExtension(srcFile.Path).ToLower();
                switch (fileNameExtension)
                {
                    case ".json": return DeserializeSrcTextFromJson(srcFile);
                    case ".csv": return DeserializeSrcTextFromCsv(srcFile);
                    default: throw new I18nTextCompileException(DiagnosticCode.UnknownFileType, $"Unknown file type ({fileNameExtension}) as an I18n Text source file.", srcFile.Path);
                }
            }
            catch (Exception ex)
            {
                options.LogError(ex);
                return new I18nTextTable(Enumerable.Empty<KeyValuePair<string, string>>());
            }
        }

        private static I18nTextTable DeserializeSrcTextFromJson(I18nTextSourceFile srcFile)
        {
            // NOTE:
            // a JSON.NET old version has problem that it can't deserialize ConcurrentDictionary directly.
            // Therefore, deserialize into normal dictionary at first, and second, re - constrauct as ConcurrentDictionary.
            try
            {
                var tableTextRaw = JsonConvert.DeserializeObject<Dictionary<string, string>>(srcFile.GetText());
                return new I18nTextTable(tableTextRaw);
            }
            catch (JsonReaderException e)
            {
                throw new I18nTextCompileException(DiagnosticCode.SourceTextIsInvalidFormat, e.Message, srcFile.Path,
                    lineNumber: e.LineNumber,
                    linePos: e.LinePosition);
            }
            catch (Exception e)
            {
                throw new I18nTextCompileException(DiagnosticCode.SourceTextIsInvalidFormat, e.Message, srcFile.Path);
            }
        }

        internal class KeyValue
        {
            public string Key { get; set; }

            public string Value { get; set; }
        }

        internal class CsvKeyValueMapping : CsvMapping<KeyValue>
        {
            public CsvKeyValueMapping() : base()
            {
                this.MapProperty(0, x => x.Key);
                this.MapProperty(1, x => x.Value);
            }
        }

        private static I18nTextTable DeserializeSrcTextFromCsv(I18nTextSourceFile srcFile)
        {
            var csvParser = new CsvParser<KeyValue>(
                new CsvParserOptions(skipHeader: false, fieldsSeparator: ','),
                new CsvKeyValueMapping());

            var srcText = srcFile.GetText();
            var keyValuePairs = new Dictionary<string, string>();
            var rows = csvParser.ReadFromString(new CsvReaderOptions(new[] { "\r\n", "\n" }), srcText);
            foreach (var row in rows)
            {
                if (!row.IsValid) throw new I18nTextCompileException(DiagnosticCode.SourceTextIsInvalidFormat, "Invalid CSV format", srcFile.Path, row.RowIndex);
                keyValuePairs.Add(row.Result.Key, row.Result.Value);
            }
            return new I18nTextTable(keyValuePairs);
        }
    }
}
