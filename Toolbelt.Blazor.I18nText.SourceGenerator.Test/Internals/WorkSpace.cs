using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;
public class WorkSpace : IDisposable
{
    public WorkDirectory ProjectDir { get; }

    public string I18nTextDir { get; }

    public string TypesDir { get; }

    public string TextResJsonsDir { get; }

    public string RootNamespace { get; }

    public string I18nTextNamespace { get; }

    public WorkSpace()
    {
        this.RootNamespace = "Toolbelt.Blazor.I18nTextCompileTask.Test";
        this.I18nTextNamespace = this.RootNamespace + ".I18nText";

        var testProjDir = FileIO.FindContainerDirToAncestor("*.csproj");
        this.ProjectDir = new WorkDirectory();
        this.I18nTextDir = Path.Combine(this.ProjectDir, "i18ntext");
        this.TypesDir = Path.Combine(this.I18nTextDir, "@types");
        this.TextResJsonsDir = Path.Combine(this.ProjectDir, "obj", "Debug", "net6.0", "dist", "_content", "i18ntext");

        FileIO.XcopyDir(Path.Combine(testProjDir, "i18ntext"), this.I18nTextDir);
    }

    public GeneratorExecutionContext CreateGeneratorExecutionContext(
        bool useSourceGenerator = true,
        string i18nTextSourceDirectory = "i18ntext",
        string fallbackLang = "en",
        bool disableSubNameSpace = false,
        Func<AdditionalText, bool>? filterAdditionalFiles = null
    )
    {
        var configOptionsProvider = new TestConfigOptionsProvider()
            .ConfigureGlobalOptions(globalOptions => globalOptions
                .Add("build_property.ProjectDir", this.ProjectDir)
                .Add("build_property.RootNamespace", this.RootNamespace)
                .Add("build_property.I18nTextNamespace", this.I18nTextNamespace)
                .Add("build_property.I18nTextSourceDirectory", i18nTextSourceDirectory)
                .Add("build_property.I18nTextFallBackLanguage", fallbackLang)
                .Add("build_property.I18nTextDisableSubNameSpace", disableSubNameSpace.ToString().ToLower())
                .Add("build_property.I18nTextIntermediateDir", this.TextResJsonsDir)
                .Add("build_property.I18nTextUseSourceGenerator", useSourceGenerator.ToString().ToLower())
            );

        filterAdditionalFiles ??= _ => true;
        var additionalFiles = Directory.GetFiles(this.I18nTextDir, "*.*", SearchOption.AllDirectories)
            .Select(path => new AdditionalTextImplement(path))
            .Cast<AdditionalText>()
            .Where(filterAdditionalFiles)
            .ToImmutableArray();

        var context = (GeneratorExecutionContext)Activator.CreateInstance(typeof(GeneratorExecutionContext), BindingFlags.NonPublic | BindingFlags.Instance, null, new object?[] {
            default(Compilation),
            default(ParseOptions),
            additionalFiles,
            configOptionsProvider,
            default(ISyntaxReceiver),
            default(CancellationToken)
        }, default(CultureInfo))!;

        return context;
    }

    public void Dispose()
    {
        this.ProjectDir.Dispose();
    }
}
