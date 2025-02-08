﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

public class WorkSpace : IDisposable
{
    public WorkDirectory ProjectDir { get; }

    /// <summary>
    /// Gets the directory path of the Localized Text Source files.<br/>
    /// By default, it is "~/i18ntext".
    /// </summary>
    public string I18nTextDir { get; }

    /// <summary>
    /// Get the the output directory path of the generated Localized Text Resource JSON files.<br/>
    /// it is "~/obj/Debug/net8.0/dist/_content/i18ntext".
    /// </summary>
    public string TextResJsonsDir { get; }

    /// <summary>
    /// Gets the root namespace of the project.<br/>
    /// "Toolbelt.Blazor.I18nText.SourceGenerator.Test" by default.
    /// </summary>
    public string RootNamespace { get; }

    public string I18nTextNamespace { get; }

    public WorkSpace(string i18ntextSourceDir = "i18ntext")
    {
        this.RootNamespace = "Toolbelt.Blazor.I18nText.SourceGenerator.Test";
        this.I18nTextNamespace = this.RootNamespace + ".I18nText";

        var testProjDir = FileIO.FindContainerDirToAncestor("*.csproj");
        this.ProjectDir = new WorkDirectory();
        this.I18nTextDir = Path.Combine(this.ProjectDir, "i18ntext");
        this.TextResJsonsDir = Path.Combine(this.ProjectDir, Path.Combine("obj/Debug/net8.0/dist/_content/i18ntext".Split('/')));

        FileIO.XcopyDir(Path.Combine(testProjDir, i18ntextSourceDir), this.I18nTextDir);
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

        var context = DynamicBinder.CreateInstance<GeneratorExecutionContext>(
            default(Compilation),
            default(ParseOptions),
            additionalFiles,
            configOptionsProvider,
            default(ISyntaxReceiver),
            ".cs",
            default(CancellationToken));
        return context;
    }

    public void Dispose()
    {
        this.ProjectDir.Dispose();
    }
}
