using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toolbelt.Blazor.I18nText.Compiler.Shared;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;

namespace Toolbelt.Blazor.I18nText.CompileTask
{
    internal class LocalizedTextResourceJsonGenerator
    {
        internal bool Generate(IEnumerable<I18nTextSourceFile> srcFiles, I18nTextCompilerOptions options, CancellationToken cancellationToken)
        {
            try
            {
                var i18textSrc = I18nTextSourceFile.Parse(srcFiles, options, cancellationToken);
                GenerateLocalizedTextResourceJsonFiles(options, i18textSrc, cancellationToken);
                return true;
            }
            catch (AggregateException e) when (e.InnerException is I18nTextCompileException compileException)
            {
                options.LogError(compileException);
                return false;
            }
            catch (I18nTextCompileException compileException)
            {
                options.LogError(compileException);
                return false;
            }
        }

        private static void GenerateLocalizedTextResourceJsonFiles(I18nTextCompilerOptions options, I18nTextSource i18textSrc, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!i18textSrc.Types.Any()) return;
            if (!Directory.Exists(options.OutDirectory)) Directory.CreateDirectory(options.OutDirectory);

            var types = i18textSrc.Types
                .SelectMany(type => type.Value.Langs.Select(lang => (lang, jsonPath: Path.Combine(options.OutDirectory, options.NameSpace + "." + type.Key + "." + lang.Key + ".json"))))
                .ToArray();

            // Sweep old generated/should be purge text json files.
            var existsTextJsonFiles = Directory.GetFiles(options.OutDirectory, "*.json");
            var shouldBeSweepedFiles = existsTextJsonFiles.Except(types.Select(t => t.jsonPath));
            foreach (var shouldBeSweepedFile in shouldBeSweepedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Delete(shouldBeSweepedFile);
            }

            Parallel.ForEach(types, new ParallelOptions { CancellationToken = cancellationToken }, ((KeyValuePair<string, I18nTextTable> lang, string jsonPath) arg) =>
            {
                var textTable = new SortedDictionary<string, string>(arg.lang.Value);
                var jsonText = JsonConvert.SerializeObject(textTable, Formatting.Indented);
                cancellationToken.ThrowIfCancellationRequested();

                var skipOutput = false;
                if (File.Exists(arg.jsonPath))
                {
                    var prevJsonText = File.ReadAllText(arg.jsonPath);
                    cancellationToken.ThrowIfCancellationRequested();
                    skipOutput = prevJsonText == jsonText;
                }

                if (!skipOutput) File.WriteAllText(arg.jsonPath, jsonText);
            });
        }
    }
}
