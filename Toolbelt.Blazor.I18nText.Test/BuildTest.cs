using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

using static Toolbelt.Blazor.I18nText.Test.Internals.Shell;

namespace Toolbelt.Blazor.I18nText.Test
{
    public class BuildTest
    {
        public BuildTest()
        {
            UpdateLibraryProjects();
        }

        private static HashSet<string> DirOfUpdatedProj = new HashSet<string>();

        private static bool CheckAndMarkUpdated(string projectDir)
        {
            lock (DirOfUpdatedProj)
            {
                if (DirOfUpdatedProj.Contains(projectDir)) return true;
                DirOfUpdatedProj.Add(projectDir);
                return false;
            }
        }

        private static void UpdateLibraryProjects()
        {
            var testDir = GetTestDir();
            foreach (var libName in new[] { "Lib4PackRef", "Lib4ProjRef" })
            {
                var libDir = Path.Combine(testDir, libName);
                var libProjPath = Path.Combine(libDir, libName + ".csproj");
                var libProjDocBefore = XDocument.Load(libProjPath);
                var i18textVersionBefore = libProjDocBefore.Descendants("PackageReference").Where(node => node.Attribute("Include").Value == "Toolbelt.Blazor.I18nText").First().Attribute("Version").Value;

                if (CheckAndMarkUpdated(libDir)) continue;

                Run(libDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText")
                    .ExitCode.Is(0);

                var libProjDocAfter = XDocument.Load(libProjPath);
                var i18textVersionAfter = libProjDocAfter.Descendants("PackageReference").Where(node => node.Attribute("Include").Value == "Toolbelt.Blazor.I18nText").First().Attribute("Version").Value;
                if (i18textVersionBefore != i18textVersionAfter)
                {
                    var libVerNode = libProjDocAfter.Descendants("Version").First();
                    var libCurVer = Version.Parse(libVerNode.Value);
                    var libNextVer = $"{libCurVer.ToString(3)}.{libCurVer.Revision + 1}";
                    libVerNode.Value = libNextVer;
                    libProjDocAfter.Save(libProjPath);
                }

                Run(libDir, "dotnet", "build").ExitCode.Is(0);
            }
        }

        private static string GetTestDir()
        {
            var testDir = AppDomain.CurrentDomain.BaseDirectory;
            do { testDir = Path.GetDirectoryName(testDir); } while (!Exists(testDir, "*.sln"));
            testDir = Path.Combine(testDir, "Tests");
            return testDir;
        }

        private static (string StartupProj, string ClientProj, string Bin, string Obj) GetDirectories(string startupProjDir, string clientProjDir)
        {
            var testDir = GetTestDir();
            startupProjDir = Path.Combine(testDir, startupProjDir);
            clientProjDir = Path.Combine(testDir, clientProjDir);
            var binDir = Path.Combine(startupProjDir, "bin");
            var objDir = Path.Combine(startupProjDir, "obj");

            return (
                StartupProj: startupProjDir,
                ClientProj: clientProjDir,
                Bin: binDir,
                Obj: objDir);
        }

        public static IEnumerable<object[]> Projects = new[] {
            new object[]{ "Client", "netstandard2.0", "Client"},
            new object[]{ "Server", "netcoreapp3.0", "Client"},
            // TODO: Currently Server Side Blazor doesn't support Blazor library feature.
            // new object[]{"ServerSideApp", "ServerSideApp.Server", "netcoreapp2.1",  "ServerSideApp.App"},
        };

        [Theory(DisplayName = "Build")]
        [MemberData(nameof(Projects))]
        public void BasicBuildTest(string startupProjDir, string platform, string clientProjDir)
        {
            var dirs = GetDirectories(startupProjDir, clientProjDir);
            var distDir = Path.Combine(dirs.Bin, Path.Combine($"Debug/{platform}/dist/_content/i18ntext".Split('/')));

            Delete(dirs.Bin);
            Delete(dirs.Obj);

            UpdatePackageRef(dirs.ClientProj);

            Run(dirs.StartupProj, "dotnet", "build")
                .ExitCode.Is(0);

            var textResJsonFileNames = Directory.GetFiles(distDir, "*.*")
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name);

            textResJsonFileNames.Is(
                "Lib4PackRef.I18nText.Text.en.json",
                "Lib4PackRef.I18nText.Text.ja.json",
                "Lib4ProjRef.I18nText.Text.en.json",
                "Lib4ProjRef.I18nText.Text.ja.json",
                "SampleSite.Client.I18nText.Text.en.json",
                "SampleSite.Client.I18nText.Text.ja.json"
            );
        }

        [Theory(DisplayName = "Publish")]
        [MemberData(nameof(Projects))]
        public void PublishTest(string startupProjDir, string platform, string clientProjDir)
        {
            var dirs = GetDirectories(startupProjDir, clientProjDir);
            var clientProjName = Path.GetFileNameWithoutExtension(Directory.GetFiles(dirs.ClientProj, "*.csproj").First());
            var distContentDir = Path.Combine(dirs.Bin, Path.Combine($"Debug/{platform}/publish/{clientProjName}/dist/_content".Split('/')));
            var i18nDistDir = Path.Combine(distContentDir, "i18ntext");

            Delete(dirs.Bin);
            Delete(dirs.Obj);

            UpdatePackageRef(dirs.ClientProj);

            Run(dirs.StartupProj, "dotnet", "publish")
                .ExitCode.Is(0);

            // Support client JavaScript file should be published into "_content/{PackageId}" folder.
            Exists(Path.Combine(distContentDir, "Toolbelt.Blazor.I18nText"), "script.js").IsTrue();

            var textResJsonFileNames = Directory.GetFiles(i18nDistDir, "*.*")
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name);

            textResJsonFileNames.Is(
                "Lib4PackRef.I18nText.Text.en.json",
                "Lib4PackRef.I18nText.Text.ja.json",
                "Lib4ProjRef.I18nText.Text.en.json",
                "Lib4ProjRef.I18nText.Text.ja.json",
                "SampleSite.Client.I18nText.Text.en.json",
                "SampleSite.Client.I18nText.Text.ja.json"
            );
        }

        private static void UpdatePackageRef(string projectDir)
        {
            if (!CheckAndMarkUpdated(projectDir))
            {
                Run(projectDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText")
                    .ExitCode.Is(0);
                Run(projectDir, "dotnet", "add", "package", "Lib4PackRef")
                    .ExitCode.Is(0);
            }
        }
    }
}
