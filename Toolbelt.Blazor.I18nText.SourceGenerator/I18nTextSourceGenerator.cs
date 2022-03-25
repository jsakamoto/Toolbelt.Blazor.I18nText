using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Toolbelt.Blazor.I18nText.SourceGenerator
{
    [Generator]
    public class I18nTextSourceGenerator : ISourceGenerator
    {
        private readonly object _lock = new object();

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var options = CreateI18nTextCompilerOptions(context);

            var i18nTextSourceDirectory = options.I18nTextSourceDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            var srcFiles = context.AdditionalFiles
                .Where(item => item.Path.StartsWith(i18nTextSourceDirectory))
                .Where(item => item.Path.EndsWith(".json", ignoreCase) || item.Path.EndsWith(".csv", ignoreCase))
                .Select(item => new I18nTextSourceFile(item.Path, Encoding.UTF8 /*item.Encoding*/))
                .ToArray();

            //foreach (var src in srcFiles) Log.LogMessage($"- {src.Path}, {src.Encoding.BodyName}");

            var successOrNot = I18nTextCompiler.Compile(srcFiles, options, saveCode: (option, item, codeLines) =>
            {
                var hintName = Path.GetFileNameWithoutExtension(item.TypeFilePath) + ".g.cs";
                lock (this._lock) context.AddSource(hintName, string.Join("\n", codeLines));
            });
        }

        private static I18nTextCompilerOptions CreateI18nTextCompilerOptions(GeneratorExecutionContext context)
        {
            var globalOptions = context.AnalyzerConfigOptions.GlobalOptions;
            //if (!globalOptions.TryGetValue("build_property.rootnamespace", out var rootNamespace)) throw new Exception("Could not determin the root namespace.");
            if (!globalOptions.TryGetValue("build_property.projectdir", out var projectDir)) throw new Exception("Could not determin the project diretcory.");
            if (!globalOptions.TryGetValue("build_property.i18ntextintermediatedir", out var i18ntextIntermediateDir)) throw new Exception("Could not determin the i18ntext intermediate directory.");
            if (!globalOptions.TryGetValue("build_property.i18ntextnamespace", out var i18ntextNamespace)) throw new Exception("Could not determin the i18ntext namespace.");
            if (!globalOptions.TryGetValue("build_property.i18ntextsourcedirectory", out var i18nTextSourceDirectory)) throw new Exception("Could not determin the i18ntext source directory.");
            if (!globalOptions.TryGetValue("build_property.i18ntextfallbacklanguage", out var fallbackLang)) throw new Exception("Could not determin the fallback language.");
            if (!globalOptions.TryGetValue("build_property.i18ntextdisablesubnamespace", out var disableSubNameSpace)) throw new Exception("Could not determin whether disable sub-namespace or not.");

            var options = new I18nTextCompilerOptions(baseDir: projectDir);
            //options.TypesDirectory = this.TypesDirectory ?? options.TypesDirectory;
            options.I18nTextSourceDirectory = Path.Combine(projectDir, i18nTextSourceDirectory);
            options.OutDirectory = i18ntextIntermediateDir;
            options.NameSpace = i18ntextNamespace;
            options.DisableSubNameSpace = bool.Parse(disableSubNameSpace);
            options.FallBackLanguage = fallbackLang;

            // TODO:
            options.LogMessage = msg => { };
            options.LogError = msg => { };

            return options;
        }
    }
}
