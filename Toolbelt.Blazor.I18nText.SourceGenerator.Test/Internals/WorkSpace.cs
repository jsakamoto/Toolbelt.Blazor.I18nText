using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;
public class WorkSpace : IDisposable
{
    public WorkDirectory ProjectDir { get; }

    public string TextResJsonsDir { get; }

    public string RootNamespace { get; }

    public string I18nTextNamespace { get; }

    private IEnumerable<AdditionalText> AdditionalFiles { get; }

    public WorkSpace()
    {
        this.RootNamespace = "Toolbelt.Blazor.I18nTextCompileTask.Test";
        this.I18nTextNamespace = this.RootNamespace + ".I18nText";

        var testProjDir = FileIO.FindContainerDirToAncestor("*.csproj");
        this.ProjectDir = new WorkDirectory();
        this.TextResJsonsDir = Path.Combine(this.ProjectDir, "obj", "Debug", "net6.0", "dist", "_content", "i18ntext");

        var i18ntextDir = Path.Combine(this.ProjectDir, "i18ntext");
        FileIO.XcopyDir(Path.Combine(testProjDir, "i18ntext"), i18ntextDir);

        this.AdditionalFiles = Directory.GetFiles(i18ntextDir, "*.*", SearchOption.AllDirectories)
            .Select(path => new AdditionalTextImplement(path))
            .Cast<AdditionalText>()
            .ToArray();
    }

    public GeneratorExecutionContext CreateGeneratorExecutionContext(
        string i18nTextSourceDirectory = "i18ntext",
        string fallbackLang = "en",
        bool disableSubNameSpace = false,
        Func<AdditionalText, bool>? filterAdditionalFiles = null
    )
    {
        var configOptionsProvider = new TestConfigOptionsProvider()
            .ConfigureGlobalOptions(globalOptions => globalOptions
                .Add("build_property.projectdir", this.ProjectDir)
                .Add("build_property.rootnamespace", this.RootNamespace)
                .Add("build_property.i18ntextnamespace", this.I18nTextNamespace)
                .Add("build_property.i18ntextsourcedirectory", i18nTextSourceDirectory)
                .Add("build_property.i18ntextfallbacklanguage", fallbackLang)
                .Add("build_property.i18ntextdisablesubnamespace", disableSubNameSpace.ToString().ToLower())
                .Add("build_property.i18ntextintermediatedir", this.TextResJsonsDir)
            );

        filterAdditionalFiles ??= _ => true;
        var additionalFiles = this.AdditionalFiles.Where(filterAdditionalFiles).ToImmutableArray();

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
