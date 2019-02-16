using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

using static Toolbelt.Blazor.I18nText.Test.Shell;

namespace Toolbelt.Blazor.I18nText.Test
{
    public class BuildTest
    {
        public BuildTest()
        {
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
