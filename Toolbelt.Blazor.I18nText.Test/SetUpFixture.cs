using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        var latestI18nTextVersion = GetLatestI18nTextPackageVersion();
        var versionOfLibProjects = await UpdateLibraryProjectsAsync("Toolbelt.Blazor.I18nText", latestI18nTextVersion);
        await BuildLibPackagesAsync();
        await UpdatePackageRefAsync(latestI18nTextVersion, versionOfLibProjects);
    }

    private static string GetLatestI18nTextPackageVersion()
    {
        var testDir = WorkSpace.GetTestDir();
        var nugetConfigPath = Path.Combine(testDir, "nuget.config");
        var nugetConfigXdoc = XDocument.Load(nugetConfigPath);
        var distDir = nugetConfigXdoc.XPathSelectElement("//packageSources/add[@key='I18nText']")?.Attribute("value")?.Value;
        if (distDir == null) throw new Exception("The package source named \"I18nText\" was not found in nuget.config.");
        var packageFiles = Directory.GetFiles(Path.Combine(testDir, distDir), "*.nupkg");

        static Version VerOf(Match m, string name) => Version.TryParse(m.Groups[name].Value, out var v) ? v : new Version(0, 0);

        var latestI18nTextVersion = packageFiles
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Select(path => Regex.Match(path, @"^Toolbelt\.Blazor\.I18nText\.(?<verText>(?<ver>[\d\.]+)(-[a-z]+\.(?<previewVer>[\d\.]+))?)$"))
            .Where(m => m.Success)
            .OrderBy(m => VerOf(m, "ver")).ThenBy(m => VerOf(m, "previewVer"))
            .Select(m => m.Groups["verText"].Value)
            .LastOrDefault();
        if (latestI18nTextVersion == null) Assert.Inconclusive("Before doing tests, the Blazor I18n text package should be packed at least once.");
        return latestI18nTextVersion;
    }

    private static Task<IReadOnlyDictionary<string, string>> UpdateLibraryProjectsAsync(string packageName, string latestPackageVersion)
    {
        var projectNames = new[] {
            Path.Combine("Lib4PackRef","Lib4PackRef"),
            Path.Combine("Lib4ProjRef", "Lib4ProjRef")};
        return UpdateReferencedPackageVersionAsync(packageName, latestPackageVersion, projectNames, restore: true, buildIfChanges: true);
    }

    private static async Task UpdatePackageRefAsync(string latestI18nTextVersion, IReadOnlyDictionary<string, string> versionOfLibProjects)
    {
        var projects = new Dictionary<string, string>
        {
            ["Components"] = Path.Combine("Components", "SampleSite.Components"),
            ["Client"] = Path.Combine("Client", "SampleSite.Client"),
            ["Server"] = Path.Combine("Server", "SampleSite.Server")
        };

        await UpdateReferencedPackageVersionAsync("Lib4PackRef", versionOfLibProjects["Lib4PackRef"], new[] { projects["Components"] }, restore: false, buildIfChanges: false);

        await UpdateReferencedPackageVersionAsync("Toolbelt.Blazor.I18nText", latestI18nTextVersion, projects.Values, restore: true, buildIfChanges: false);
    }

    private static async Task<IReadOnlyDictionary<string, string>> UpdateReferencedPackageVersionAsync(string packageName, string latestPackageVersion, IEnumerable<string> projectNames, bool buildIfChanges, bool restore)
    {
        var versionOfProjects = new Dictionary<string, string>();

        var testDir = WorkSpace.GetTestDir();

        await Parallel.ForEachAsync(projectNames, async (string projectSubPath, CancellationToken _) =>
        {
            var projectPath = Path.Combine(testDir, projectSubPath + ".csproj");
            var projectDir = Path.GetDirectoryName(projectPath);
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var projectXDoc = XDocument.Load(projectPath);

            var projectVersionNode = projectXDoc.XPathSelectElement("//Version");
            var projectCurrentVersionText = projectVersionNode?.Value ?? "1.0.0.0";
            lock (versionOfProjects) versionOfProjects.Add(projectName, projectCurrentVersionText);

            var packageRef = projectXDoc.XPathSelectElement($"//PackageReference[@Include='{packageName}']");
            var referencedVersion = packageRef?.Attribute("Version")?.Value;
            if (packageRef == null) throw new Exception($"The package reference for \"{packageName}\" was not found in the {projectName}.csproj");

            var referencedPackageVersionHasChanged = referencedVersion != latestPackageVersion;

            if (referencedPackageVersionHasChanged)
            {
                packageRef.SetAttributeValue("Version", latestPackageVersion);

                var projectCurrentVersion = Version.Parse(projectCurrentVersionText);
                var projectNextVersion = $"{projectCurrentVersion.ToString(3)}.{projectCurrentVersion.Revision + 1}";
                if (projectVersionNode != null) projectVersionNode.Value = projectNextVersion;
                lock (versionOfProjects) versionOfProjects[projectName] = projectNextVersion;

                projectXDoc.Save(projectPath);
            }

            if (restore)
            {
                await Start("dotnet", "restore", projectDir).ExitCodeIs(0);
            }

            if (referencedPackageVersionHasChanged && buildIfChanges)
            {
                await Start("dotnet", "build", projectDir).ExitCodeIs(0);
            }
        });

        return versionOfProjects;
    }

    private static async Task BuildLibPackagesAsync()
    {
        var testDir = WorkSpace.GetTestDir();
        var libPackProjects = new[] {
            Path.Combine("Lib4PackRef", "Lib4PackRef"),
            Path.Combine("Lib4PackRef6", "Lib4PackRef6"),
        };
        await Parallel.ForEachAsync(libPackProjects, async (string projectSubPath, CancellationToken _) =>
        {
            var projectPath = Path.Combine(testDir, projectSubPath + ".csproj");
            var projectDir = Path.GetDirectoryName(projectPath);
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var projectXDoc = XDocument.Load(projectPath);

            var projectVersionNode = projectXDoc.XPathSelectElement("//Version");
            var projectCurrentVersionText = projectVersionNode?.Value ?? "1.0.0.0";

            var existingLibPacks = Directory.GetFiles(Path.Combine(testDir, "_dist"), $"{projectName}.*.nupkg");
            var libPackName = $"{projectName}.{projectCurrentVersionText}.nupkg";
            var libPackExists = existingLibPacks.Any(path => Path.GetFileName(path) == libPackName);

            if (!libPackExists)
            {
                await Start("dotnet", "pack -c:Release --nologo", projectDir).ExitCodeIs(0);
            }
        });
    }
}
