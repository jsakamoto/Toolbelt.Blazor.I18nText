using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Blazor.I18nText.Test.Internals.Shell;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test
{
    [Parallelizable(ParallelScope.Children)]
    public class BuildTest
    {
        public static IEnumerable<object[]> Projects = new[] {
            // startupProjName
            new object[]{ "Client" },
            new object[]{ "Host" },
            new object[]{ "Server" },
        };

        [Test]
        [TestCaseSource(nameof(Projects))]
        public async Task BasicBuildTest(string startupProjName)
        {
            using var workSpace = WorkSpace.Create(startupProjName);
            var framework = workSpace.GetTargetFrameworkOfStartupProj();
            var distDir = Path.Combine(workSpace.Bin, Path.Combine($"Debug/{framework}/wwwroot/_content/i18ntext".Split('/')));

            using var buildProcess = await Start("dotnet", "build", workSpace.StartupProj).WaitForExitAsync();
            buildProcess.ExitCode.Is(0, message: buildProcess.Output);

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

        [Test]
        [TestCaseSource(nameof(Projects))]
        public async Task PublishTest(string startupProjName)
        {
            using var workSpace = WorkSpace.Create(startupProjName);
            var framewrok = workSpace.GetTargetFrameworkOfStartupProj();
            var publishDir = Path.Combine(workSpace.Bin, Path.Combine($"Release/{framewrok}/publish/".Split('/')));
            var wwwrootContentDir = Path.Combine(publishDir, Path.Combine($"wwwroot/_content".Split('/')));
            var i18nDistDir = Path.Combine(wwwrootContentDir, "i18ntext");
            var staticWebAssetDir = Path.Combine(wwwrootContentDir, "Toolbelt.Blazor.I18nText");

            using var publishProcess = await Start("dotnet", "publish -c:Release", workSpace.StartupProj).WaitForExitAsync();
            publishProcess.ExitCode.Is(0, message: publishProcess.Output);

            // Support client JavaScript file should be published into "_content/{PackageId}" folder.
            Exists(staticWebAssetDir, "script.min.js").IsTrue();

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
}
