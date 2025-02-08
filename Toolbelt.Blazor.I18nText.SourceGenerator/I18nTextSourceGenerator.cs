using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Toolbelt.Blazor.I18nText.Compiler.Shared;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;
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
            // In some cases, the same build error may be reported multiple times from both the source generator and localized text resource json generator.
            // To avoid this, we will keep track of the reported diagnostics and skip the duplicate ones.
            var reportedDiagnostics = new HashSet<string>();
            var logMessage = this.CreateMessageReporter(context, reportedDiagnostics);
            var logError = this.CreateErrorReporter(context, reportedDiagnostics);

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

                // Generate typed text table source code.
                var generateTypedTxtTableTask = Task.Run(() =>
                {
                    var generatedSources = new ConcurrentBag<GeneratedSource>();
                    I18nTextCompiler.Compile(srcFiles, options, saveCode: (option, item, codeLines) =>
                    {
                        var hintName = item.TypeFileName + ".g.cs";
                        var sourceCode = string.Join("\n", codeLines);
                        generatedSources.Add(new GeneratedSource(hintName, sourceCode));
                    }, cancellationToken);

                    foreach (var source in generatedSources)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        context.AddSource(source.HintName, source.SourceCode);
                    }
                }).ConfigureAwait(false);

                // Generate localized text resource json files.
                var generateI18nTextResourceJsonTask = Task.Run(() =>
                {
                    new LocalizedTextResourceJsonGenerator().Generate(srcFiles, options, cancellationToken);
                }).ConfigureAwait(false);

                // Wait for all tasks.
                generateTypedTxtTableTask.GetAwaiter().GetResult();
                generateI18nTextResourceJsonTask.GetAwaiter().GetResult();
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

            //if (!globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace)) throw new Exception("Could not determine the root namespace.");
            if (!globalOptions.TryGetValue("build_property.ProjectDir", out var projectDir)) throw new Exception("Could not determine the project directory.");
            if (!globalOptions.TryGetValue("build_property.I18nTextIntermediateDir", out var i18ntextIntermediateDir)) throw new Exception("Could not determine the i18ntext intermediate directory.");
            if (!globalOptions.TryGetValue("build_property.I18nTextNamespace", out var i18ntextNamespace)) throw new Exception("Could not determine the i18ntext namespace.");
            if (!globalOptions.TryGetValue("build_property.I18nTextSourceDirectory", out var i18nTextSourceDirectory)) throw new Exception("Could not determin the i18ntext source directory.");
            if (!globalOptions.TryGetValue("build_property.I18nTextFallBackLanguage", out var fallbackLang)) throw new Exception("Could not determine the fallback language.");
            if (!globalOptions.TryGetValue("build_property.I18nTextDisableSubNameSpace", out var disableSubNameSpace)) throw new Exception("Could not determine whether disable sub-namespace or not.");

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

        private Action<string> CreateMessageReporter(GeneratorExecutionContext context, HashSet<string> reportedDiagnostics)
        {
            return message =>
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.Information, Location.None, message);
                this.ReportDiagnostic(context, diagnostic, reportedDiagnostics);
            };
        }

        private Action<Exception> CreateErrorReporter(GeneratorExecutionContext context, HashSet<string> reportedDiagnostics)
        {
            return ex => this.ReportException(context, ex, reportedDiagnostics);
        }

        private void ReportException(GeneratorExecutionContext context, Exception ex, HashSet<string> reportedDiagnostics)
        {
            if (ex is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    this.ReportException(context, innerException, reportedDiagnostics);
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

                var diagnostic = Diagnostic.Create(descriptor ?? DiagnosticDescriptors.UnhandledException, location, ex.Message);
                this.ReportDiagnostic(context, diagnostic, reportedDiagnostics);
            }
        }

        /// <summary>
        /// Report a diagnostic if it has not been reported yet.
        /// </summary>
        /// <param name="context">A generator execution context.</param>
        /// <param name="diagnostic">A diagnostic to report.</param>
        /// <param name="reportedDiagnostics">A set of reported diagnostic strings. This parameter is used to detect duplicate diagnostics that have already been reported.</param>
        private void ReportDiagnostic(GeneratorExecutionContext context, Diagnostic diagnostic, HashSet<string> reportedDiagnostics)
        {
            lock (this._lock)
            {
                var diagnosticString = diagnostic.ToString();
                if (reportedDiagnostics.Contains(diagnosticString)) return;
                reportedDiagnostics.Add(diagnosticString);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
