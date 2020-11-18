using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Blazor.I18nText.Test.Internals.Shell;

namespace Toolbelt.Blazor.I18nText.Test
{
    [Parallelizable(ParallelScope.Children)]
    public class BuildTest
    {
        private static string GetPlatform(string startupProjDir)
        {
            var projPath = Directory.GetFiles(startupProjDir, "*.csproj").First();
            return XDocument.Load(projPath).Descendants("TargetFramework").First().Value;
        }

        public static IEnumerable<object[]> Projects = new[] {
            // startupProjName
            new object[]{ "Client" },
            new object[]{ "Host" },
            new object[]{ "Server" },
        };

        [Test]
        [TestCaseSource(nameof(Projects))]
        public void BasicBuildTest(string startupProjName)
        {
            using var workSpace = WorkSpace.Create(startupProjName);
            var platform = GetPlatform(workSpace.StartupProj);
            var distDir = Path.Combine(workSpace.Bin, Path.Combine($"Debug/{platform}/wwwroot/_content/i18ntext".Split('/')));

            Run(workSpace.StartupProj, "dotnet", "build").ExitCode.Is(0);

            var textResJsonFileNames = Directory.GetFiles(distDir, "*.*")
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name);

            textResJsonFileNames.Is(
                "Lib4PackRef.I18nText.Text.en.json",
                "Lib4PackRef.I18nText.Text.ja.json",
                "Lib4ProjRef.I18nText.Text.en.json",
                "Lib4ProjRef.I18nText.Text.ja.json",
                "SampleSite.Components.I18nText.Text.en.json",
                "SampleSite.Components.I18nText.Text.ja.json"
            );
        }

        [Test]
        [TestCaseSource(nameof(Projects))]
        public void PublishTest(string startupProjName)
        {
            using var workSpace = WorkSpace.Create(startupProjName);
            var platform = GetPlatform(workSpace.StartupProj);
            var publishDir = Path.Combine(workSpace.Bin, Path.Combine($"Release/{platform}/publish/".Split('/')));
            var wwwrootContentDir = Path.Combine(publishDir, Path.Combine($"wwwroot/_content".Split('/')));
            var i18nDistDir = Path.Combine(wwwrootContentDir, "i18ntext");
            var staticWebAssetDir = Path.Combine(wwwrootContentDir, "Toolbelt.Blazor.I18nText");

            Run(workSpace.StartupProj, "dotnet", "publish", "-c:Release").ExitCode.Is(0);

            // Support client JavaScript file should be published into "_content/{PackageId}" folder.
            Exists(staticWebAssetDir, "script.min.js").IsTrue();

            var textResJsonFileNames = Directory.GetFiles(i18nDistDir, "*.json")
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name);

            textResJsonFileNames.Is(
                "Lib4PackRef.I18nText.Text.en.json",
                "Lib4PackRef.I18nText.Text.ja.json",
                "Lib4ProjRef.I18nText.Text.en.json",
                "Lib4ProjRef.I18nText.Text.ja.json",
                "SampleSite.Components.I18nText.Text.en.json",
                "SampleSite.Components.I18nText.Text.ja.json"
            );
        }
    }
}
