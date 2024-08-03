using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Toolbelt.Blazor.I18nText.SourceGenerator.Internals;

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
            var logMessage = this.CreateMessageReporter(context);
            var logError = this.CreateErrorReporter(context);

            try
            {
                var options = this.CreateI18nTextCompilerOptions(context, logMessage, logError);
                if (options == null) return;

                var cancellationToken = context.CancellationToken;

                var i18nTextSourceDirectory = options.I18nTextSourceDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
                var srcFiles = context.AdditionalFiles
                    .Where(item => item.Path.StartsWith(i18nTextSourceDirectory))
                    .Where(item => item.Path.EndsWith(".json", ignoreCase) || item.Path.EndsWith(".csv", ignoreCase))
                    .Select(item => new I18nTextSourceFile(item.Path, Encoding.UTF8 /*item.Encoding*/))
                    .ToArray();

                //foreach (var src in srcFiles) Log.LogMessage($"- {src.Path}, {src.Encoding.BodyName}");

                var generatedSources = new ConcurrentBag<GeneratedSource>();
                var successOrNot = I18nTextCompiler.Compile(srcFiles, options, saveCode: (option, item, codeLines) =>
                {
                    var hintName = Path.GetFileNameWithoutExtension(item.TypeFilePath) + ".g.cs";
                    var sourceCode = string.Join("\n", codeLines);
                    generatedSources.Add(new GeneratedSource(hintName, sourceCode));
                }, cancellationToken);

                I18nTextCompiler.SweepTypeFilesShouldBePurged(options, Enumerable.Empty<I18nTextCompileItem>(), cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                foreach (var source in generatedSources)
                {
                    context.AddSource(source.HintName, source.SourceCode);
                }
            }
            catch (Exception e)
            {
                logError(e);
            }
        }

        private I18nTextCompilerOptions CreateI18nTextCompilerOptions(GeneratorExecutionContext context, Action<string> logMessage, Action<Exception> logError)
        {
            var globalOptions = context.AnalyzerConfigOptions.GlobalOptions;
            if (!globalOptions.TryGetValue("build_property.I18nTextUseSourceGenerator", out var _)) return null;

            //if (!globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace)) throw new Exception("Could not determin the root namespace.");
            if (!globalOptions.TryGetValue("build_property.ProjectDir", out var projectDir)) throw new Exception("Could not determin the project diretcory.");
            if (!globalOptions.TryGetValue("build_property.I18nTextIntermediateDir", out var i18ntextIntermediateDir)) throw new Exception("Could not determin the i18ntext intermediate directory.");
            if (!globalOptions.TryGetValue("build_property.I18nTextNamespace", out var i18ntextNamespace)) throw new Exception("Could not determin the i18ntext namespace.");
            if (!globalOptions.TryGetValue("build_property.I18nTextSourceDirectory", out var i18nTextSourceDirectory)) throw new Exception("Could not determin the i18ntext source directory.");
            if (!globalOptions.TryGetValue("build_property.I18nTextFallBackLanguage", out var fallbackLang)) throw new Exception("Could not determin the fallback language.");
            if (!globalOptions.TryGetValue("build_property.I18nTextDisableSubNameSpace", out var disableSubNameSpace)) throw new Exception("Could not determin whether disable sub-namespace or not.");

            return new I18nTextCompilerOptions(baseDir: projectDir)
            {
                I18nTextSourceDirectory = Path.Combine(projectDir, i18nTextSourceDirectory),
                OutDirectory = i18ntextIntermediateDir,
                NameSpace = i18ntextNamespace,
                DisableSubNameSpace = bool.Parse(disableSubNameSpace),
                FallBackLanguage = fallbackLang,

                LogMessage = logMessage,
                LogError = logError
            };
        }

        private Action<string> CreateMessageReporter(GeneratorExecutionContext context)
        {
            return message =>
            {
                lock (this._lock) context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Information, Location.None, message));
            };
        }

        private Action<Exception> CreateErrorReporter(GeneratorExecutionContext context)
        {
            return ex => this.ReportException(context, ex);
        }

        private void ReportException(GeneratorExecutionContext context, Exception ex)
        {
            if (ex is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    this.ReportException(context, innerException);
                }
            }
            else
            {
                var location = Location.None;
                var descriptor = default(DiagnosticDescriptor);

                if (ex is I18nTextCompileException compileException)
                {
                    if (!string.IsNullOrEmpty(compileException.FilePath))
                    {
                        location = Location.Create(compileException.FilePath, default, new LinePositionSpan(
                            start: new LinePosition(Math.Max(compileException.LineNumber - 1, 0), compileException.LinePos),
                            end: new LinePosition(Math.Max(compileException.LineNumber - 1, 0), compileException.LinePos)));
                    }

                    DiagnosticDescriptors.TryGetByCode(compileException.Code, out descriptor);
                }

                descriptor ??= DiagnosticDescriptors.UnhandledException;
                lock (this._lock) context.ReportDiagnostic(Diagnostic.Create(descriptor, location, ex.Message));
            }
        }
    }
}
