using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test;

[Parallelizable(ParallelScope.All)]
public class BuildTest
{
    private static readonly IEnumerable<string> HostingModels = new[] {
        "Client", "Host", "Server" };

    private static readonly IEnumerable<string> Frameworks = new[] {
        "net6.0", "net7.0" };

    public static readonly IEnumerable<object[]> Projects =
        from startupProjName in HostingModels
        from framework in Frameworks
        select new object[] { startupProjName, framework };

    [Test, TestCaseSource(nameof(Projects))]
    public async Task BasicBuildTest(string startupProjName, string framework)
    {
        using var workSpace = new WorkSpace(startupProjName, framework, configuration: "Debug");
        var distDir = Path.Combine(workSpace.OutputDir, Path.Combine("wwwroot/_content/i18ntext".Split('/')));

        await Start("dotnet", $"build -f:{framework}", workSpace.StartupProj).ExitCodeIs(0);

        var textResJsonFileNames = Directory.GetFiles(distDir, "*.*")
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name);

        textResJsonFileNames.Is(
            "Lib4PackRef.I18nText.Text.en.json",
            "Lib4PackRef.I18nText.Text.ja.json",
            "Lib4PackRef6.I18nText.Text.en.json",
            "Lib4PackRef6.I18nText.Text.ja.json",
            "Lib4ProjRef.I18nText.Text.en.json",
            "Lib4ProjRef.I18nText.Text.ja.json",
            "SampleSite.Components.I18nText.Text.en.json",
            "SampleSite.Components.I18nText.Text.ja.json"
        );
    }

    [Test, TestCaseSource(nameof(Projects))]
    public async Task PublishTest(string startupProjName, string framework)
    {
        using var workSpace = new WorkSpace(startupProjName, framework, configuration: "Release");
        var wwwrootContentDir = Path.Combine(workSpace.PublishDir, "wwwroot", "_content");
        var i18nDistDir = Path.Combine(wwwrootContentDir, "i18ntext");
        var staticWebAssetDir = Path.Combine(wwwrootContentDir, "Toolbelt.Blazor.I18nText");

        await Start("dotnet", $"publish -c:Release -f:{framework} -p:UsingBrowserRuntimeWorkload=false --nologo", workSpace.StartupProj).ExitCodeIs(0);

        // Support client JavaScript file should be published into "_content/{PackageId}" folder.
        FileIO.ExistsAnyFilesInDir(staticWebAssetDir, "script.min.js").IsTrue();

        var textResJsonFileNames = Directory.GetFiles(i18nDistDir, "*.json")
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name);

        textResJsonFileNames.Is(
            "Lib4PackRef.I18nText.Text.en.json",
            "Lib4PackRef.I18nText.Text.ja.json",
            "Lib4PackRef6.I18nText.Text.en.json",
            "Lib4PackRef6.I18nText.Text.ja.json",
            "Lib4ProjRef.I18nText.Text.en.json",
            "Lib4ProjRef.I18nText.Text.ja.json",
            "SampleSite.Components.I18nText.Text.en.json",
            "SampleSite.Components.I18nText.Text.ja.json"
        );
    }
}
