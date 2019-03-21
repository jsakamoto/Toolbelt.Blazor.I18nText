using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

using static Toolbelt.Blazor.I18nText.Test.Shell;

namespace Toolbelt.Blazor.I18nText.Test
{
    public class BuildTest
    {
        public BuildTest()
        {
            UpdateLibraryProjects();
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

                Run(libDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText");
                ErrorLevel.Is(0);

                var libProjDocAfter = XDocument.Load(libProjPath);
                var i18textVersionAfter = libProjDocAfter.Descendants("PackageReference").Where(node => node.Attribute("Include").Value == "Toolbelt.Blazor.I18nText").First().Attribute("Version").Value;
                if (i18textVersionBefore != i18textVersionAfter)
                {
                    var libVerNode = libProjDocAfter.Descendants("Version").First();
                    var libCurVer = Version.Parse(libVerNode.Value);
                    var libNextVer = $"1.0.{libCurVer.Build + 1}";
                    libVerNode.Value = libNextVer;
                    libProjDocAfter.Save(libProjPath);
                }

                Run(libDir, "dotnet", "build");
                ErrorLevel.Is(0);
            }
        }

        private static string GetTestDir()
        {
            var testDir = AppDomain.CurrentDomain.BaseDirectory;
            do { testDir = Path.GetDirectoryName(testDir); } while (!Exists(testDir, "*.sln"));
            testDir = Path.Combine(testDir, "Tests");
            return testDir;
        }

        public static IEnumerable<object[]> Projects = new[] {
            new object[]{"StandAloneApp", "StandAloneApp",        "netstandard2.0", "StandAloneApp"},
            new object[]{"HostedApp",     "HostedApp.Server",     "netcoreapp3.0",  "HostedApp.Client"},
            // TODO: Currently ASP.NET Core Razor Component (a.k.a Server Side Blazor) doesn't support Blazor library feature.
            // new object[]{"ServerSideApp", "ServerSideApp.Server", "netcoreapp2.1",  "ServerSideApp.App"},
        };

        [Theory(DisplayName = "Build")]
        [MemberData(nameof(Projects))]
        public void BasicBuildTest(string solutionName, string stratupProjName, string platform, string clientProjName)
        {
            var testDir = GetTestDir();
            var startupProjDir = Path.Combine(testDir, solutionName, stratupProjName);
            var clientProjDir = Path.Combine(testDir, solutionName, clientProjName);

            var binDir = Path.Combine(startupProjDir, "bin");
            var objDir = Path.Combine(startupProjDir, "obj");
            var distDir = Path.Combine(binDir, Path.Combine($"Debug/{platform}/dist/_content/i18ntext".Split('/')));

            Delete(binDir);
            Delete(objDir);

            Run(clientProjDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText");
            ErrorLevel.Is(0);
            Run(clientProjDir, "dotnet", "add", "package", "Lib4PackRef");
            ErrorLevel.Is(0);
            Run(testDir, "dotnet", "build", $"{solutionName}.sln");
            ErrorLevel.Is(0);

            Exists(distDir, "Lib4PackRef.I18nText.Text.en.json").IsTrue();
            Exists(distDir, "Lib4PackRef.I18nText.Text.ja.json").IsTrue();
            Exists(distDir, "Lib4ProjRef.I18nText.Text.en.json").IsTrue();
            Exists(distDir, "Lib4ProjRef.I18nText.Text.ja.json").IsTrue();
        }

        [Theory(DisplayName = "Publish")]
        [MemberData(nameof(Projects))]
        public void PublishTest(string solutionName, string stratupProjName, string platform, string clientProjName)
        {
            var testDir = GetTestDir();
            var startupProjDir = Path.Combine(testDir, solutionName, stratupProjName);
            var clientProjDir = Path.Combine(testDir, solutionName, clientProjName);

            var binDir = Path.Combine(startupProjDir, "bin");
            var objDir = Path.Combine(startupProjDir, "obj");
            var distDir = Path.Combine(binDir, Path.Combine($"Debug/{platform}/publish/{clientProjName}/dist/content/i18ntext".Split('/')));

            Delete(binDir);
            Delete(objDir);

            Run(clientProjDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText");
            ErrorLevel.Is(0);
            Run(clientProjDir, "dotnet", "add", "package", "Lib4PackRef");
            ErrorLevel.Is(0);
            Run(testDir, "dotnet", "publish", $"{solutionName}.sln");
            ErrorLevel.Is(0);

            Exists(distDir, "Lib4PackRef.I18nText.Text.en.json").IsTrue();
            Exists(distDir, "Lib4PackRef.I18nText.Text.ja.json").IsTrue();
            Exists(distDir, "Lib4ProjRef.I18nText.Text.en.json").IsTrue();
            Exists(distDir, "Lib4ProjRef.I18nText.Text.ja.json").IsTrue();
        }
    }

    public static class Shell
    {
        public static int ErrorLevel { get; set; }

        public static bool Exists(string dir, string wildCard)
        {
            return Directory.GetFiles(dir, wildCard, SearchOption.TopDirectoryOnly).Any();
        }

        public static void Delete(string dir)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }

        public static int Run(string workDir, params string[] args)
        {
            var pi = new ProcessStartInfo
            {
                WorkingDirectory = workDir,
                FileName = args.First(),
                Arguments = string.Join(" ", args.Skip(1)),
                UseShellExecute = false,
            };
            var process = Process.Start(pi);
            process.WaitForExit();
            ErrorLevel = process.ExitCode;
            return process.ExitCode;
        }
    }
}
